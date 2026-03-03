# Glossario de Termos - Sistema de Compra Programada de Acoes

Este glossario apresenta os termos utilizados no desafio tecnico, organizados dos conceitos mais basicos aos mais complexos, para facilitar o entendimento do enunciado.

---

## Termos Basicos

### Acao
Menor fracao do capital social de uma empresa de capital aberto. Ao comprar uma acao, o investidor se torna socio da empresa, participando dos seus resultados (lucros ou prejuizos).

### Ticker (Codigo do Ativo)
Codigo alfanumerico que identifica uma acao na bolsa de valores. Exemplos: `PETR4` (Petrobras PN), `VALE3` (Vale ON), `ITUB4` (Itau Unibanco PN). O numero ao final indica o tipo da acao (3 = Ordinaria, 4 = Preferencial).

### B3 (Brasil, Bolsa, Balcao)
Bolsa de valores oficial do Brasil, onde sao negociadas acoes, fundos, derivativos e outros ativos financeiros. Antiga BM&FBovespa.

### Pregao
Sessao de negociacao da bolsa de valores. Na B3, o pregao ocorre em dias uteis, geralmente das 10h as 17h (horario de Brasilia). Cada dia de negociacao e chamado de "dia de pregao".

### Cotacao
Preco de uma acao em um determinado momento. Existem varios tipos de cotacao ao longo de um pregao:
- **Cotacao de Abertura:** Preco do primeiro negocio do dia
- **Cotacao de Fechamento:** Preco do ultimo negocio do dia (utilizado neste sistema)
- **Cotacao Maxima:** Maior preco atingido no dia
- **Cotacao Minima:** Menor preco atingido no dia

### Dia Util
Dia em que ha pregao na bolsa. Para simplificacao neste desafio, considere dias uteis como **segunda a sexta-feira** (excluindo sabados e domingos).

### Aporte
Valor financeiro que o investidor deposita/investe. No contexto deste sistema, e o valor mensal que o cliente define para investir na carteira recomendada.

---

## Termos Intermediarios

### Lote Padrao
Quantidade minima de acoes para negociacao no mercado principal da B3. O lote padrao e de **100 acoes**. Para comprar no mercado de lote padrao, a quantidade deve ser multipla de 100. Exemplo: 100, 200, 300 acoes.

### Mercado Fracionario
Segmento de negociacao da B3 que permite a compra e venda de **1 a 99 acoes** de um ativo, ou seja, quantidades menores que o lote padrao. O ticker do ativo no mercado fracionario recebe o sufixo **"F"**. Exemplo: `PETR4F` para negociar fracoes de Petrobras PN.

### Carteira de Acoes (Portfolio)
Conjunto de acoes que um investidor possui. Cada ativo e sua respectiva quantidade compoe a carteira.

### Carteira Recomendada (Top Five)
Selecao de 5 acoes indicadas pela equipe de Research (analistas de mercado) da Itau Corretora, com um percentual de alocacao definido para cada ativo. Exemplo: PETR4 (30%), VALE3 (25%), ITUB4 (20%), BBDC4 (15%), WEGE3 (10%).

### Custodia
Registro formal da posse de ativos financeiros por um investidor. A custodia indica quais acoes e em que quantidade pertencem a cada titular. No sistema, existem dois tipos:
- **Custodia Master:** Ativos sob posse da conta da corretora (posicao consolidada)
- **Custodia Filhote:** Ativos sob posse de cada cliente individual

### Conta Grafica
Conta de registro escritural mantida pela corretora em nome do cliente. Nao e uma conta bancaria; e o registro que associa o cliente as suas posicoes de ativos (custodia). No sistema, cada cliente recebe uma "conta grafica filhote" ao aderir ao produto.

### Conta Master
Conta da propria corretora onde as compras sao realizadas de forma consolidada antes de serem distribuidas aos clientes. Funciona como um "intermediario" entre o mercado e as contas individuais dos clientes.

### Arquivo COTAHIST
Arquivo historico de cotacoes disponibilizado pela B3 diariamente. Contem informacoes detalhadas de todos os ativos negociados em cada pregao (preco de abertura, fechamento, maxima, minima, volume, etc.). O formato e um arquivo texto (TXT) com layout de campos fixos definido pela B3.

---

## Termos Avancados

### Compra Programada
Modalidade de investimento onde o cliente define um valor fixo mensal e o sistema automaticamente executa as compras de acoes de forma periodica e recorrente, seguindo uma estrategia pre-definida (neste caso, a carteira Top Five).

### Distribuicao (Rateio)
Processo de alocar os ativos comprados na conta master para as contas filhotes de cada cliente, proporcionalmente ao valor de aporte de cada um. Exemplo: se o Cliente A representa 33% do total investido e o Cliente B 67%, a distribuicao dos ativos segue essa proporcao.

### Residuo
Quantidade de acoes que sobra na conta master apos a distribuicao para todos os clientes, geralmente causada por arredondamentos (nao e possivel distribuir fracoes de acoes). O residuo e mantido na custodia master e considerado na proxima compra.

### Preco Medio de Aquisicao (PM)
Custo medio ponderado de compra de um ativo por um investidor, recalculado a cada nova compra. E essencial para calcular lucro ou prejuizo em vendas.

**Formula:**
```
PM = (Qtd Anterior x PM Anterior + Qtd Nova x Preco Nova Compra) / (Qtd Anterior + Qtd Nova)
```

**Exemplo:**
- Compra 1: 100 acoes de PETR4 a R$ 35,00 => PM = R$ 35,00
- Compra 2: 50 acoes de PETR4 a R$ 38,00 => PM = (100 x 35 + 50 x 38) / 150 = R$ 36,00

### Rebalanceamento
Ajuste na composicao da carteira do cliente para realinha-la aos percentuais da cesta recomendada. Ocorre em dois cenarios:

1. **Rebalanceamento por mudanca de recomendacao:** A equipe de Research altera as acoes ou percentuais da cesta Top Five. Ativos que sairam da recomendacao devem ser vendidos e os novos ativos devem ser comprados.

2. **Rebalanceamento por desvio de proporcao:** Quando a valorizacao ou desvalorizacao de um ativo causa uma divergencia significativa entre a proporcao real da carteira do cliente e a proporcao alvo da cesta. Exemplo: se PETR4 deveria representar 30% mas por valorizacao passou a representar 45%, e necessario vender parte de PETR4 e comprar mais dos ativos sub-representados.

### P/L (Lucro/Prejuizo - Profit/Loss)
Indicador que mostra o resultado financeiro de um investimento. Pode ser calculado por ativo ou para a carteira total.

**Formula por ativo:**
```
P/L = (Cotacao Atual - Preco Medio de Aquisicao) x Quantidade
```

**Exemplo:**
- O investidor possui 100 acoes de PETR4 com PM de R$ 35,00
- Cotacao atual: R$ 40,00
- P/L = (40,00 - 35,00) x 100 = **R$ 500,00 (lucro)**

Quando P/L e positivo, o investidor esta com **lucro**. Quando negativo, esta com **prejuizo**.

### Rentabilidade
Percentual de retorno sobre o investimento. Indica quanto o investimento rendeu em relacao ao capital aplicado.

**Formula:**
```
Rentabilidade (%) = ((Valor Atual da Carteira - Valor Total Investido) / Valor Total Investido) x 100
```

**Exemplo:**
- Valor total investido: R$ 10.000,00
- Valor atual da carteira: R$ 11.500,00
- Rentabilidade = ((11.500 - 10.000) / 10.000) x 100 = **15%**

### Saldo
Valor financeiro total da carteira do cliente em um dado momento, calculado pela soma do valor de mercado de todos os ativos em custodia.

**Formula:**
```
Saldo = Somatorio de (Quantidade de cada ativo x Cotacao atual de cada ativo)
```

---

## Termos Fiscais e Regulatorios

### IR (Imposto de Renda)
Tributo federal que incide sobre a renda e os proventos de contribuintes. No mercado de acoes, incide sobre o **lucro** obtido na venda de ativos.

### IR Dedo-Duro (IRRF - Imposto de Renda Retido na Fonte)
Apelido para o Imposto de Renda Retido na Fonte que incide sobre operacoes de renda variavel. A aliquota e de **0,005%** sobre o valor total da operacao de venda.

**Porque "dedo-duro"?** Porque esse imposto serve como mecanismo de rastreamento da Receita Federal. Mesmo sendo um valor pequeno, ele "denuncia" (aponta o dedo) que o investidor realizou operacoes na bolsa, permitindo que a Receita cruze informacoes.

**Exemplo:**
- Venda de R$ 10.000,00 em acoes
- IR dedo-duro = R$ 10.000,00 x 0,005% = **R$ 0,50**

O valor retido pode ser descontado do IR total devido na apuracao mensal.

### Isencao de IR para Pessoa Fisica
Regra fiscal que **isenta** pessoas fisicas do pagamento de Imposto de Renda sobre lucro em vendas de acoes quando o **total de vendas no mes nao ultrapassa R$ 20.000,00**.

- **Vendas no mes <= R$ 20.000,00:** Isento de IR sobre o lucro
- **Vendas no mes > R$ 20.000,00:** Incide **20% de IR sobre o lucro liquido** de todas as vendas do mes

**Exemplo 1 (Isento):**
- Total de vendas no mes: R$ 15.000,00
- Lucro: R$ 3.000,00
- IR devido: **R$ 0,00** (isento, pois vendas < R$ 20.000)

**Exemplo 2 (Tributado):**
- Total de vendas no mes: R$ 25.000,00
- Lucro: R$ 5.000,00
- IR devido: R$ 5.000,00 x 20% = **R$ 1.000,00**

### Lucro Liquido (em vendas de acoes)
Diferenca entre o valor de venda e o custo de aquisicao (pelo preco medio) dos ativos vendidos.

**Formula:**
```
Lucro Liquido = Valor de Venda - (Quantidade Vendida x Preco Medio de Aquisicao)
```

**Exemplo:**
- Venda de 100 acoes de VALE3 a R$ 70,00 = R$ 7.000,00
- Preco medio de aquisicao: R$ 60,00
- Custo de aquisicao: 100 x R$ 60,00 = R$ 6.000,00
- Lucro liquido: R$ 7.000,00 - R$ 6.000,00 = **R$ 1.000,00**

---

## Termos de Tecnologia e Arquitetura

### API REST
Interface de programacao de aplicacoes que segue o padrao REST (Representational State Transfer), utilizando metodos HTTP (GET, POST, PUT, DELETE) para comunicacao entre sistemas.

### Swagger / OpenAPI
Ferramenta de documentacao interativa para APIs REST. Permite visualizar e testar os endpoints da API diretamente pelo navegador. No .NET Core, pode ser configurado automaticamente via pacote `Swashbuckle`.

### Apache Kafka
Plataforma de streaming de eventos distribuida, utilizada para comunicacao assincrona entre sistemas (mensageria). Funciona com o conceito de **topicos** (canais de mensagens) onde **produtores** publicam mensagens e **consumidores** as leem.

No sistema, o Kafka e utilizado para publicar os eventos de IR dedo-duro, simulando o envio de informacoes a Receita Federal.

### Topico Kafka
Canal/fila de mensagens dentro do Kafka. Produtores enviam mensagens para um topico, e consumidores leem mensagens desse topico. Exemplo: topico `ir-dedo-duro` recebe os eventos de IR de cada operacao.

### Docker / Docker Compose
Plataforma de containerizacao que permite empacotar aplicacoes e suas dependencias em containers isolados. O **Docker Compose** permite definir e executar multiplos containers (ex: Kafka + MySQL) com um unico comando (`docker-compose up`).

---

*Este glossario e parte do material de apoio do Desafio Tecnico de Compra Programada de Acoes da Itau Corretora.*
