# Desafio Tecnico - Sistema de Compra Programada de Acoes

## Itau Corretora - Engenharia de Software

---

## 1. Visao Geral do Produto

Voce devera desenvolver um **Sistema de Compra Programada de Acoes** para a Itau Corretora. O produto permite que clientes adiram a um plano de investimento recorrente e automatizado em uma carteira recomendada de **5 acoes** (chamada **"Top Five"**), definida pela equipe de Research da Itau Corretora.

O cliente escolhe um **valor mensal de aporte**, e o sistema automaticamente:

- Executa as compras de acoes de forma consolidada (na conta master da corretora)
- Distribui os ativos proporcionalmente para a custodia individual de cada cliente
- Gerencia rebalanceamentos quando a composicao da carteira recomendada muda ou quando ha desvios significativos de proporcao

---

## 2. Conceitos Importantes do Mercado Financeiro

Antes de iniciar o desenvolvimento, e fundamental compreender os seguintes conceitos:

### 2.1. Lote Padrao vs. Mercado Fracionario

Na B3 (Bolsa de Valores do Brasil), as acoes podem ser negociadas de duas formas:

- **Lote Padrao (Mercado Primario):** As acoes sao negociadas em lotes de **100 unidades**. Por exemplo, para comprar acoes da PETR4 no mercado de lote padrao, voce deve comprar multiplos de 100 (100, 200, 300...). O ticker utilizado e o codigo padrao do ativo (ex: `PETR4`).

- **Mercado Fracionario:** Permite a compra de **1 a 99 unidades** de uma acao. O ticker no mercado fracionario recebe o sufixo `F` (ex: `PETR4F`). Isso possibilita investimentos com valores menores, pois nao e necessario comprar o lote inteiro.

**No sistema:** As compras devem considerar ambos os mercados. Ao calcular a quantidade de acoes a comprar, o sistema deve priorizar lotes padrao (multiplos de 100) quando possivel e utilizar o mercado fracionario para o restante. Exemplo: se o calculo resultar em 350 acoes de PETR4, devem ser comprados 3 lotes padrao (300 acoes via `PETR4`) + 50 acoes fracionarias (via `PETR4F`).

### 2.2. IR "Dedo-Duro" (Imposto de Renda Retido na Fonte)

O "dedo-duro" e o apelido para o **Imposto de Renda Retido na Fonte (IRRF)** que incide sobre operacoes de renda variavel. A aliquota e de **0,005%** sobre o valor total da operacao de venda.

Este imposto e retido automaticamente pela corretora e serve como um mecanismo de rastreamento da Receita Federal para identificar operacoes realizadas pelo investidor. O valor retido pode ser descontado do IR devido na apuracao mensal.

**No sistema:** A cada operacao de compra distribuida ao cliente, o sistema deve calcular o valor do IR dedo-duro e publicar essa informacao em um topico Kafka.

### 2.3. Isencao de IR para Pessoa Fisica em Vendas de Acoes

Pessoas fisicas sao **isentas** de Imposto de Renda sobre o lucro de vendas de acoes quando o **total de vendas no mes nao ultrapassa R$ 20.000,00**.

- Se o total de vendas no mes **exceder R$ 20.000,00**, incide **20% de imposto sobre o lucro liquido** de todas as vendas do mes.
- O lucro e calculado como: `Valor de Venda - (Quantidade * Preco Medio de Aquisicao)`

**No sistema:** Ao realizar rebalanceamentos que envolvam vendas, o sistema deve verificar se o total de vendas do cliente no mes ultrapassa R$ 20.000,00 e, em caso positivo, calcular e recolher o IR de 20% sobre o lucro.

### 2.4. Preco Medio de Aquisicao

O preco medio de aquisicao e o custo medio ponderado de compra de um ativo por um investidor. Ele e fundamental para:

- Calcular o **lucro ou prejuizo** em vendas (para fins de IR)
- Acompanhar a **rentabilidade** da carteira

**Formula:**

```
Preco Medio = (Quantidade Anterior * Preco Medio Anterior + Quantidade Nova * Preco Nova Compra) / (Quantidade Anterior + Quantidade Nova)
```

**No sistema:** O preco medio deve ser mantido e atualizado a cada compra, por ativo, por cliente.

### 2.5. Arquivo COTAHIST da B3

A B3 disponibiliza diariamente arquivos com as cotacoes historicas de todos os ativos negociados, chamado **COTAHIST**. Este arquivo contem informacoes como:

- Codigo do ativo
- Data do pregao
- Preco de abertura, fechamento, maximo e minimo
- Volume negociado

**Como obter o arquivo:**

1. Acesse o site da B3: [https://www.b3.com.br/pt_br/market-data-e-indices/servicos-de-dados/market-data/historico/mercado-a-vista/cotacoes-historicas/](https://www.b3.com.br/pt_br/market-data-e-indices/servicos-de-dados/market-data/historico/mercado-a-vista/cotacoes-historicas/)
2. Selecione o periodo desejado (Diario, Mensal ou Anual)
3. Faca o download do arquivo no formato TXT
4. O layout do arquivo segue a especificacao documentada pela B3 (disponivel no mesmo site)

**No sistema:** O candidato deve implementar a leitura e parse do arquivo COTAHIST. Os arquivos devem ser armazenados em uma pasta chamada `cotacoes/` na raiz do projeto, com o nome do arquivo referente a cada pregao (ex: `COTAHIST_D20260225.TXT`). A cotacao utilizada para calculo das compras deve ser a **cotacao de fechamento** do ultimo pregao disponivel.

---

## 3. Arquitetura e Entidades do Sistema

### 3.1. Entidades Principais

| Entidade | Descricao |
|---|---|
| **Cliente** | Pessoa que adere ao produto. Possui dados cadastrais e valor mensal de aporte |
| **Conta Master** | Conta da corretora que consolida as compras antes da distribuicao |
| **Conta Grafica (Filhote)** | Conta individual criada para cada cliente no momento da adesao |
| **Custodia Master** | Posicao de ativos remanescentes na conta master apos a distribuicao |
| **Custodia Filhote** | Posicao de ativos de cada cliente, vinculada a sua conta grafica |
| **Cesta de Recomendacao (Top Five)** | Conjunto de 5 acoes com o percentual de cada uma, definido pelo administrador |
| **Ordem de Compra** | Registro de compra consolidada na conta master |
| **Distribuicao** | Registro da alocacao de ativos da conta master para as contas filhotes |

### 3.2. Diagrama de Relacionamento (simplificado)

```
                    [Administrador]
                          |
                    [Cesta Top Five]
                    (5 acoes + %)
                          |
[Cliente] ---adesao---> [Conta Grafica Filhote] ---> [Custodia Filhote]
                                                          ^
                                                          |
                                                    (distribuicao)
                                                          |
                          [Conta Master] ----------> [Custodia Master]
                                |
                          (compra consolidada)
                                |
                          [Arquivo COTAHIST B3]
```

---

## 4. Funcionalidades Obrigatorias

### 4.1. Interface do Cliente (API REST)

| Endpoint | Descricao |
|---|---|
| **Adesao ao produto** | Cliente adere ao produto informando seus dados e o valor mensal de aporte. O sistema cria a conta grafica e custodia filhote |
| **Saida do produto** | Cliente solicita sair do produto. O sistema interrompe novas compras, mas **mantem a posicao existente** na custodia filhote |
| **Alterar valor mensal** | Cliente altera o valor do aporte mensal |
| **Consultar carteira** | Cliente visualiza sua custodia: ativos, quantidades, preco medio, valor atual, P/L (lucro/prejuizo) e rentabilidade |
| **Acompanhamento de rentabilidade** | Tela/endpoint que exibe informacoes detalhadas de rentabilidade da carteira do cliente, incluindo: saldo total, P/L por ativo, P/L total, rentabilidade percentual, historico de evolucao, e demais informacoes pertinentes ao acompanhamento de rentabilidade de uma carteira em corretora |

### 4.2. Painel Administrativo (API REST)

| Endpoint | Descricao |
|---|---|
| **Cadastrar/Alterar Cesta Top Five** | Registra as 5 acoes recomendadas e o percentual de cada uma na cesta (a soma dos percentuais deve ser 100%) |
| **Visualizar cesta atual** | Retorna a composicao atual da cesta de recomendacao |
| **Historico de cestas** | Retorna o historico de alteracoes da cesta |

### 4.3. Motor de Compra Programada

O motor de compra e o coracao do sistema. Ele deve executar o seguinte fluxo nos **dias uteis iguais ou subsequentes ao dia 5, 15 e 25 de cada mes** (considerar dias uteis como segunda a sexta-feira para simplificacao):

#### Passo a passo:

1. **Agrupamento de pedidos:** Coletar todos os clientes ativos e calcular **1/3 do valor mensal** de cada cliente para a data corrente (o valor mensal e dividido em 3 parcelas: dia 5, dia 15 e dia 25)

2. **Calculo da compra consolidada:** Somar os valores de todos os clientes e calcular a quantidade de cada ativo a comprar segundo os percentuais da cesta Top Five vigente, utilizando a **cotacao de fechamento** do ultimo pregao disponivel no arquivo COTAHIST

3. **Consideracao do saldo da custodia master:** Antes de emitir a ordem de compra, verificar se ha saldo remanescente na custodia master para cada ativo. Se houver, descontar do total a comprar

4. **Execucao da compra:** Registrar a compra na conta master (priorizar lotes padrao e utilizar mercado fracionario para o restante)

5. **Distribuicao para contas filhotes:** Distribuir os ativos comprados para cada custodia filhote proporcionalmente ao valor do aporte de cada cliente

6. **Residuos:** Caso apos a distribuicao total ainda existam ativos remanescentes na conta master (por arredondamentos ou fracoes), esses ativos devem ser mantidos na custodia master para serem considerados na proxima data de compra

7. **IR dedo-duro:** Para cada distribuicao ao cliente, calcular o IR dedo-duro (0,005% sobre o valor da operacao) e publicar em um **topico Kafka** com as informacoes necessarias para eventual envio a Receita Federal

### 4.4. Motor de Rebalanceamento

O rebalanceamento deve ocorrer em duas situacoes:

#### A) Mudanca na composicao da cesta Top Five

Quando o administrador altera a cesta de recomendacao:
1. Identificar os ativos que **sairam** da cesta
2. Para cada cliente, **vender** a posicao dos ativos que sairam
3. Com o valor obtido, **comprar** os novos ativos segundo a nova composicao
4. Atualizar a custodia filhote de cada cliente

#### B) Rebalanceamento por desvio de proporcao

Quando a valorizacao ou desvalorizacao de um ativo causa um desvio significativo na proporcao da carteira do cliente em relacao a cesta recomendada:
1. Calcular a proporcao atual de cada ativo na carteira do cliente
2. Comparar com os percentuais da cesta Top Five
3. Vender ativos que estao acima da proporcao alvo
4. Comprar ativos que estao abaixo da proporcao alvo

#### Regras de IR no rebalanceamento:
- Somar todas as vendas do cliente no mes corrente
- Se o total de vendas **exceder R$ 20.000,00**, calcular **20% de IR sobre o lucro liquido** (diferenca entre valor de venda e custo de aquisicao pelo preco medio)
- Publicar o valor do IR no topico Kafka

---

## 5. Requisitos Tecnicos

### 5.1. Stack Obrigatoria

| Componente | Tecnologia |
|---|---|
| **Backend** | .NET Core (C#) |
| **Banco de Dados** | MySQL |
| **Mensageria** | Apache Kafka (instancia real, subida via Docker) |
| **API** | REST com documentacao **Swagger/OpenAPI** |
| **Cotacoes** | Arquivo COTAHIST da B3 (leitura e parse do arquivo TXT) |

### 5.2. Requisitos de Qualidade

- **Cobertura de testes:** Minimo de **70%** de cobertura de testes unitarios e/ou de integracao
- **Codigo limpo:** Seguir boas praticas de Clean Code, SOLID e design patterns adequados
- **Documentacao:** README.md completo com instrucoes de como rodar o projeto, arquitetura utilizada e decisoes tecnicas

### 5.3. Diferenciais (nao obrigatorios, mas valorizados)

- Interface web (frontend) para o cliente e/ou painel administrativo (livre escolha de framework)
- Uso de padroes como CQRS, Event Sourcing, Domain-Driven Design (DDD)
- Observabilidade (logs estruturados, metricas)
- CI/CD configurado no repositorio

---

## 6. Estrutura do Projeto

```
/
|-- cotacoes/                  # Pasta com arquivos COTAHIST da B3
|   |-- COTAHIST_D20260225.TXT
|   |-- COTAHIST_D20260226.TXT
|   +-- ...
|-- src/                       # Codigo-fonte do sistema
|-- tests/                     # Testes unitarios e de integracao
|-- docker-compose.yml         # Kafka + MySQL (no minimo)
|-- README.md                  # Documentacao do projeto
+-- ...
```

---

## 7. Exemplo de Fluxo Completo

Para facilitar o entendimento, segue um exemplo pratico:

### Cenario:
- **Cesta Top Five vigente:**

| Acao | Percentual |
|---|---|
| PETR4 | 30% |
| VALE3 | 25% |
| ITUB4 | 20% |
| BBDC4 | 15% |
| WEGE3 | 10% |

- **Cliente A:** Aporte mensal de R$ 3.000,00
- **Cliente B:** Aporte mensal de R$ 6.000,00
- **Data:** Dia 5 do mes (dia util)

### Execucao:

1. **Calculo do 1/3:**
   - Cliente A: R$ 3.000 / 3 = R$ 1.000,00
   - Cliente B: R$ 6.000 / 3 = R$ 2.000,00
   - **Total consolidado: R$ 3.000,00**

2. **Calculo por ativo (usando cotacao de fechamento):**

   | Acao | % Cesta | Valor a Comprar | Cotacao Fechamento | Qtd a Comprar |
   |---|---|---|---|---|
   | PETR4 | 30% | R$ 900,00 | R$ 38,50 | 23 acoes |
   | VALE3 | 25% | R$ 750,00 | R$ 62,00 | 12 acoes |
   | ITUB4 | 20% | R$ 600,00 | R$ 30,00 | 20 acoes |
   | BBDC4 | 15% | R$ 450,00 | R$ 15,00 | 30 acoes |
   | WEGE3 | 10% | R$ 300,00 | R$ 40,00 | 7 acoes |

3. **Compra na conta master** (considerando saldo residual = 0 neste exemplo)

4. **Distribuicao:**
   - Cliente A (1/3 de R$ 3.000 = R$ 1.000 => ~33,33% do total):
     - PETR4: ~7 acoes, VALE3: ~4 acoes, ITUB4: ~6 acoes, BBDC4: ~10 acoes, WEGE3: ~2 acoes
   - Cliente B (1/3 de R$ 6.000 = R$ 2.000 => ~66,67% do total):
     - PETR4: ~15 acoes, VALE3: ~8 acoes, ITUB4: ~13 acoes, BBDC4: ~20 acoes, WEGE3: ~4 acoes
   - **Residuos** ficam na custodia master (ex: 1 acao de PETR4, 1 acao de ITUB4)

5. **IR dedo-duro:** Publicar no Kafka o valor de 0,005% sobre cada operacao distribuida

---

## 8. Criterios de Avaliacao

| Criterio | Peso |
|---|---|
| **Funcionalidade completa** - Todas as funcionalidades implementadas e funcionando | Alto |
| **Qualidade do codigo** - Clean code, SOLID, design patterns, organizacao | Alto |
| **Arquitetura** - Decisoes tecnicas justificadas e bem estruturadas | Alto |
| **Testes** - Cobertura minima de 70%, testes significativos | Alto |
| **Modelagem de dados** - Entidades bem definidas, relacionamentos corretos | Medio |
| **Dominio de negocio** - Entendimento correto das regras de mercado financeiro | Medio |
| **Documentacao** - README claro, Swagger completo, decisoes documentadas | Medio |
| **Diferenciais** - Frontend, DDD, CQRS, observabilidade, CI/CD | Bonus |

---

## 9. Entrega

O candidato deve:

1. **Publicar o codigo** em um repositorio **publico** no seu **GitHub pessoal**
2. **Produzir um video** demonstrando:
   - O sistema funcionando com todas as funcionalidades
   - A logica e o raciocinio utilizado na implementacao
   - A arquitetura escolhida e o porquê das decisoes
   - Quaisquer outros pontos que julgar importantes
3. **Publicar o video** no seu **YouTube pessoal** com visibilidade **publica**
4. **Enviar um e-mail** para **guilherme.camarao@icloud.com** contendo:
   - Link do seu perfil no **LinkedIn**
   - Link do **video no YouTube**
   - Link do **repositorio no GitHub**

---

## 10. Duvidas

Em caso de duvidas sobre as regras de negocio ou requisitos tecnicos, envie um e-mail para **guilherme.camarao@icloud.com** com o assunto **"Duvida - Desafio Tecnico Compra Programada"**.

---

**Boa sorte!**
