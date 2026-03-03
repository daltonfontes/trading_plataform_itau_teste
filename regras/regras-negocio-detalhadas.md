# Regras de Negocio Detalhadas

## Sistema de Compra Programada de Acoes - Itau Corretora

Este documento detalha todas as regras de negocio do sistema, incluindo casos de borda e exemplos numericos completos.

---

## 1. Regras de Adesao e Gestao do Cliente

### 1.1. Adesao ao Produto

| Regra | Descricao |
|---|---|
| RN-001 | O cliente deve informar: Nome, CPF, Email e Valor Mensal de Aporte |
| RN-002 | O CPF deve ser unico no sistema (nao pode haver duplicidade) |
| RN-003 | O valor mensal minimo deve ser validado (sugestao: R$ 100,00 minimo) |
| RN-004 | Ao aderir, o sistema deve criar automaticamente uma **Conta Grafica Filhote** e uma **Custodia Filhote** vinculadas ao cliente |
| RN-005 | O cliente inicia com status **Ativo = true** |
| RN-006 | A data de adesao deve ser registrada |

### 1.2. Saida do Produto

| Regra | Descricao |
|---|---|
| RN-007 | Ao sair, o status do cliente muda para **Ativo = false** |
| RN-008 | A posicao existente na custodia filhote e **mantida** (nao ha venda automatica) |
| RN-009 | O cliente nao participa mais das compras programadas (excluido do agrupamento) |
| RN-010 | O cliente pode consultar sua carteira mesmo apos sair |

### 1.3. Alteracao do Valor Mensal

| Regra | Descricao |
|---|---|
| RN-011 | O cliente pode alterar o valor mensal a qualquer momento |
| RN-012 | Se a alteracao ocorrer entre datas de compra, o novo valor sera usado na proxima data de compra |
| RN-013 | O valor anterior deve ser mantido em historico |

**Exemplo - Caso de Borda:**
- Cliente A tem aporte de R$ 3.000/mes
- No dia 7, altera para R$ 6.000/mes
- No dia 5, ja foi usada R$ 1.000 (1/3 de R$ 3.000)
- No dia 15, sera usada R$ 2.000 (1/3 de R$ 6.000 - novo valor)
- No dia 25, sera usada R$ 2.000 (1/3 de R$ 6.000)

---

## 2. Regras da Cesta de Recomendacao (Top Five)

### 2.1. Cadastro e Alteracao

| Regra | Descricao |
|---|---|
| RN-014 | A cesta deve conter exatamente **5 acoes** |
| RN-015 | A soma dos percentuais deve ser exatamente **100%** |
| RN-016 | Cada percentual deve ser maior que 0% |
| RN-017 | Ao alterar a cesta, a cesta anterior deve ser **desativada** (DataDesativacao preenchida) e uma nova cesta **ativa** deve ser criada |
| RN-018 | Apenas **uma cesta pode estar ativa** por vez |
| RN-019 | A alteracao da cesta deve disparar o processo de **rebalanceamento** para todos os clientes ativos |

**Exemplo:**
```
Cesta Antiga (desativada):
  PETR4: 30%, VALE3: 25%, ITUB4: 20%, BBDC4: 15%, WEGE3: 10%

Cesta Nova (ativa):
  PETR4: 25%, VALE3: 20%, ITUB4: 20%, ABEV3: 20%, RENT3: 15%

Mudancas:
  - BBDC4 SAIU (era 15%) => VENDER posicao de todos os clientes
  - WEGE3 SAIU (era 10%) => VENDER posicao de todos os clientes
  - ABEV3 ENTROU (20%) => COMPRAR para todos os clientes
  - RENT3 ENTROU (15%) => COMPRAR para todos os clientes
  - PETR4 mudou de 30% para 25% => REBALANCEAR
  - VALE3 mudou de 25% para 20% => REBALANCEAR
  - ITUB4 manteve 20% => SEM ALTERACAO
```

---

## 3. Regras do Motor de Compra Programada

### 3.1. Datas de Execucao

| Regra | Descricao |
|---|---|
| RN-020 | As compras devem ocorrer em **3 datas por mes**: dias 5, 15 e 25 |
| RN-021 | Se o dia 5, 15 ou 25 cair em um **sabado ou domingo**, a compra deve ser executada no **proximo dia util** (segunda-feira) |
| RN-022 | Dias uteis sao simplificados como **segunda a sexta-feira** |
| RN-023 | O valor mensal e dividido em **3 parcelas iguais** (1/3 por data) |

**Exemplo - Calendario:**
```
Fevereiro 2026:
  Dia 5 (quinta) => Executa dia 5
  Dia 15 (domingo) => Executa dia 16 (segunda)
  Dia 25 (quarta) => Executa dia 25
```

### 3.2. Agrupamento de Pedidos

| Regra | Descricao |
|---|---|
| RN-024 | Apenas clientes com **Ativo = true** participam do agrupamento |
| RN-025 | O valor de cada cliente para a data e: `ValorMensal / 3` |
| RN-026 | Os valores sao consolidados (somados) para compra unica na conta master |

**Exemplo Numerico Completo:**
```
Clientes Ativos:
  Cliente A: R$ 3.000/mes => R$ 1.000 por data
  Cliente B: R$ 6.000/mes => R$ 2.000 por data
  Cliente C: R$ 1.500/mes => R$ 500 por data

Total Consolidado: R$ 3.500

Cesta Vigente:
  PETR4: 30% | VALE3: 25% | ITUB4: 20% | BBDC4: 15% | WEGE3: 10%

Valor por Ativo (consolidado):
  PETR4: R$ 3.500 x 30% = R$ 1.050,00
  VALE3: R$ 3.500 x 25% = R$ 875,00
  ITUB4: R$ 3.500 x 20% = R$ 700,00
  BBDC4: R$ 3.500 x 15% = R$ 525,00
  WEGE3: R$ 3.500 x 10% = R$ 350,00
```

### 3.3. Calculo de Quantidade a Comprar

| Regra | Descricao |
|---|---|
| RN-027 | Usar a **cotacao de fechamento** do ultimo pregao disponivel no arquivo COTAHIST |
| RN-028 | Quantidade = **TRUNCAR(Valor / Cotacao)** (arredondar para baixo, inteiro) |
| RN-029 | Antes de comprar, verificar se ha **saldo na custodia master** (residuos anteriores) |
| RN-030 | Se houver saldo master, **descontar** da quantidade a comprar |

**Exemplo Numerico (continuacao):**
```
Cotacoes de Fechamento (ultimo pregao):
  PETR4: R$ 35,00 | VALE3: R$ 62,00 | ITUB4: R$ 30,00 | BBDC4: R$ 15,00 | WEGE3: R$ 40,00

Saldo Custodia Master (residuos anteriores):
  PETR4: 2 acoes | VALE3: 0 | ITUB4: 1 acao | BBDC4: 0 | WEGE3: 0

Quantidade a comprar (sem saldo):
  PETR4: TRUNCAR(1.050 / 35) = 30 acoes
  VALE3: TRUNCAR(875 / 62) = 14 acoes
  ITUB4: TRUNCAR(700 / 30) = 23 acoes
  BBDC4: TRUNCAR(525 / 15) = 35 acoes
  WEGE3: TRUNCAR(350 / 40) = 8 acoes

Quantidade a comprar (descontando saldo master):
  PETR4: 30 - 2 = 28 acoes a comprar
  VALE3: 14 - 0 = 14 acoes a comprar
  ITUB4: 23 - 1 = 22 acoes a comprar
  BBDC4: 35 - 0 = 35 acoes a comprar
  WEGE3: 8 - 0 = 8 acoes a comprar
```

### 3.4. Separacao Lote Padrao vs Fracionario

| Regra | Descricao |
|---|---|
| RN-031 | Quantidades >= 100 devem ser compradas em **lote padrao** (multiplos de 100) |
| RN-032 | O restante (1-99 acoes) deve ser comprado no **mercado fracionario** |
| RN-033 | No fracionario, o ticker recebe sufixo "F" (ex: `PETR4F`) |

**Exemplo (continuacao):**
```
PETR4: 28 acoes => 0 lotes (0 via PETR4) + 28 fracionarias (PETR4F)
VALE3: 14 acoes => 0 lotes + 14 fracionarias (VALE3F)
ITUB4: 22 acoes => 0 lotes + 22 fracionarias (ITUB4F)
BBDC4: 35 acoes => 0 lotes + 35 fracionarias (BBDC4F)
WEGE3: 8 acoes => 0 lotes + 8 fracionarias (WEGE3F)

Outro exemplo com mais volume:
  Se fossem 350 acoes de PETR4:
  => 3 lotes (300 via PETR4) + 50 fracionarias (PETR4F)
```

### 3.5. Distribuicao para Custodias Filhotes

| Regra | Descricao |
|---|---|
| RN-034 | A distribuicao e proporcional ao **aporte de cada cliente** em relacao ao total |
| RN-035 | Proporcao do cliente = `Aporte do Cliente / Total de Aportes` |
| RN-036 | Quantidade por cliente = **TRUNCAR(Proporcao x Quantidade Total Disponivel)** |
| RN-037 | A quantidade total disponivel = compradas + saldo master anterior |
| RN-038 | Ao distribuir, o **preco medio** da custodia filhote deve ser atualizado |

**Exemplo Numerico (continuacao):**
```
Total de aportes: R$ 3.500
  Cliente A: R$ 1.000 => 28,57%
  Cliente B: R$ 2.000 => 57,14%
  Cliente C: R$ 500 => 14,29%

Total de PETR4 disponivel: 28 (compradas) + 2 (saldo master) = 30

Distribuicao PETR4:
  Cliente A: TRUNCAR(30 x 28,57%) = TRUNCAR(8,57) = 8 acoes
  Cliente B: TRUNCAR(30 x 57,14%) = TRUNCAR(17,14) = 17 acoes
  Cliente C: TRUNCAR(30 x 14,29%) = TRUNCAR(4,29) = 4 acoes
  Total distribuido: 8 + 17 + 4 = 29 acoes
  Residuo na master: 30 - 29 = 1 acao de PETR4

Distribuicao VALE3 (14 disponiveis):
  Cliente A: TRUNCAR(14 x 28,57%) = TRUNCAR(4,00) = 4 acoes
  Cliente B: TRUNCAR(14 x 57,14%) = TRUNCAR(8,00) = 8 acoes
  Cliente C: TRUNCAR(14 x 14,29%) = TRUNCAR(2,00) = 2 acoes
  Total distribuido: 4 + 8 + 2 = 14 acoes
  Residuo na master: 0 acoes

Distribuicao ITUB4 (22 + 1 saldo = 23 disponiveis):
  Cliente A: TRUNCAR(23 x 28,57%) = TRUNCAR(6,57) = 6 acoes
  Cliente B: TRUNCAR(23 x 57,14%) = TRUNCAR(13,14) = 13 acoes
  Cliente C: TRUNCAR(23 x 14,29%) = TRUNCAR(3,29) = 3 acoes
  Total distribuido: 6 + 13 + 3 = 22 acoes
  Residuo na master: 23 - 22 = 1 acao de ITUB4

Distribuicao BBDC4 (35 disponiveis):
  Cliente A: TRUNCAR(35 x 28,57%) = TRUNCAR(10,00) = 10 acoes
  Cliente B: TRUNCAR(35 x 57,14%) = TRUNCAR(20,00) = 20 acoes
  Cliente C: TRUNCAR(35 x 14,29%) = TRUNCAR(5,00) = 5 acoes
  Total distribuido: 10 + 20 + 5 = 35 acoes
  Residuo na master: 0 acoes

Distribuicao WEGE3 (8 disponiveis):
  Cliente A: TRUNCAR(8 x 28,57%) = TRUNCAR(2,29) = 2 acoes
  Cliente B: TRUNCAR(8 x 57,14%) = TRUNCAR(4,57) = 4 acoes
  Cliente C: TRUNCAR(8 x 14,29%) = TRUNCAR(1,14) = 1 acao
  Total distribuido: 2 + 4 + 1 = 7 acoes
  Residuo na master: 8 - 7 = 1 acao de WEGE3
```

### 3.6. Residuos

| Regra | Descricao |
|---|---|
| RN-039 | Acoes nao distribuidas permanecem na **custodia master** |
| RN-040 | Na proxima data de compra, o saldo da custodia master deve ser **considerado** (descontado do total a comprar) |

**Resumo de residuos do exemplo:**
```
Custodia Master apos distribuicao:
  PETR4: 1 acao
  VALE3: 0
  ITUB4: 1 acao
  BBDC4: 0
  WEGE3: 1 acao

Na proxima compra (dia 15), esses saldos serao descontados.
```

---

## 4. Regras de Preco Medio

| Regra | Descricao |
|---|---|
| RN-041 | O preco medio deve ser calculado por **ativo por cliente** |
| RN-042 | Formula: `PM = (Qtd Anterior x PM Anterior + Qtd Nova x Preco Nova) / (Qtd Anterior + Qtd Nova)` |
| RN-043 | Em caso de **venda**, o preco medio **NAO se altera** (apenas a quantidade diminui) |
| RN-044 | O preco medio so e recalculado em **compras** |

**Exemplo:**
```
Cliente A - PETR4:

Compra 1 (dia 5/jan): 8 acoes a R$ 35,00
  PM = 35,00

Compra 2 (dia 15/jan): 10 acoes a R$ 37,00
  PM = (8 x 35,00 + 10 x 37,00) / (8 + 10)
  PM = (280 + 370) / 18
  PM = R$ 36,11

Venda (rebalanceamento): vende 5 acoes a R$ 40,00
  PM continua = R$ 36,11 (venda nao altera PM)
  Quantidade: 18 - 5 = 13 acoes
  Lucro na venda: 5 x (40,00 - 36,11) = R$ 19,45

Compra 3 (dia 25/jan): 7 acoes a R$ 38,00
  PM = (13 x 36,11 + 7 x 38,00) / (13 + 7)
  PM = (469,43 + 266,00) / 20
  PM = R$ 36,77
```

---

## 5. Regras de Rebalanceamento

### 5.1. Rebalanceamento por Mudanca de Cesta

| Regra | Descricao |
|---|---|
| RN-045 | Disparado quando o administrador altera a composicao da cesta Top Five |
| RN-046 | Para cada cliente ativo, identificar ativos que **sairam** da cesta |
| RN-047 | **Vender** toda a posicao dos ativos que sairam |
| RN-048 | Com o valor obtido, **comprar** os novos ativos segundo a nova composicao |
| RN-049 | Ativos que permaneceram na cesta mas mudaram de percentual devem ser **rebalanceados** (vender excesso ou comprar deficit) |

### 5.2. Rebalanceamento por Desvio de Proporcao

| Regra | Descricao |
|---|---|
| RN-050 | Ocorre quando a proporcao real da carteira diverge significativamente dos percentuais da cesta |
| RN-051 | O candidato pode definir um **limiar de desvio** (sugestao: 5 pontos percentuais) |
| RN-052 | Vender ativos **sobre-alocados** e comprar ativos **sub-alocados** |

**Exemplo Numerico - Rebalanceamento por Mudanca:**
```
Carteira do Cliente A antes da mudanca:
  PETR4: 8 acoes x R$ 35,00 = R$ 280,00 (30%)
  VALE3: 4 acoes x R$ 62,00 = R$ 248,00 (26,5%)
  ITUB4: 6 acoes x R$ 30,00 = R$ 180,00 (19,2%)
  BBDC4: 10 acoes x R$ 15,00 = R$ 150,00 (16%)
  WEGE3: 2 acoes x R$ 40,00 = R$ 80,00 (8,5%)
  Total: R$ 938,00

Nova cesta: PETR4(25%), VALE3(20%), ITUB4(20%), ABEV3(20%), RENT3(15%)

Acoes que SAIRAM: BBDC4, WEGE3
Acoes que ENTRARAM: ABEV3, RENT3

Passo 1 - Vender ativos que sairam:
  Vender 10 BBDC4 x R$ 15,00 = R$ 150,00
  Vender 2 WEGE3 x R$ 40,00 = R$ 80,00
  Total obtido com vendas: R$ 230,00

Passo 2 - Comprar novos ativos (proporcionalmente):
  ABEV3: 20% da cesta => 20/(20+15) = 57,14% do valor de venda
    R$ 230 x 57,14% = R$ 131,43
    Se ABEV3 = R$ 14,00 => TRUNCAR(131,43 / 14) = 9 acoes

  RENT3: 15% da cesta => 15/(20+15) = 42,86% do valor de venda
    R$ 230 x 42,86% = R$ 98,57
    Se RENT3 = R$ 48,00 => TRUNCAR(98,57 / 48) = 2 acoes

Passo 3 - Rebalancear ativos que mudaram de percentual:
  (Ajustar PETR4 de 30% para 25% e VALE3 de 25% para 20%)
  Valor alvo total: R$ 938,00
  PETR4 alvo: R$ 938 x 25% = R$ 234,50 => 6 acoes (tem 8, vender 2)
  VALE3 alvo: R$ 938 x 20% = R$ 187,60 => 3 acoes (tem 4, vender 1)
```

---

## 6. Regras Fiscais (IR)

### 6.1. IR Dedo-Duro

| Regra | Descricao |
|---|---|
| RN-053 | Aliquota: **0,005%** sobre o valor total da operacao |
| RN-054 | Calculado para **cada operacao** distribuida ao cliente |
| RN-055 | O valor deve ser publicado em um **topico Kafka** |
| RN-056 | A mensagem Kafka deve conter: ClienteId, CPF, Ticker, Valor Operacao, Valor IR, Data |

**Exemplo:**
```
Distribuicao para Cliente A:
  8 acoes PETR4 x R$ 35,00 = R$ 280,00
  IR dedo-duro = R$ 280,00 x 0,005% = R$ 0,014 => R$ 0,01

  4 acoes VALE3 x R$ 62,00 = R$ 248,00
  IR dedo-duro = R$ 248,00 x 0,005% = R$ 0,0124 => R$ 0,01
```

### 6.2. IR sobre Vendas (Rebalanceamento)

| Regra | Descricao |
|---|---|
| RN-057 | Somar **todas as vendas** do cliente no **mes corrente** |
| RN-058 | Se o total de vendas <= R$ 20.000,00: **ISENTO** |
| RN-059 | Se o total de vendas > R$ 20.000,00: **20% sobre o lucro liquido total** |
| RN-060 | Lucro = Valor de Venda - (Quantidade x Preco Medio) |
| RN-061 | Se houver **prejuizo**, o IR e R$ 0,00 (prejuizo pode ser compensado em meses futuros, mas para simplificacao do exercicio, nao e necessario implementar compensacao) |
| RN-062 | O IR deve ser publicado no topico Kafka |

**Exemplo 1 - Isento:**
```
Cliente A - Rebalanceamento em janeiro:
  Venda: 10 BBDC4 x R$ 15,00 = R$ 150,00
  Venda: 2 WEGE3 x R$ 40,00 = R$ 80,00
  Total vendas no mes: R$ 230,00

  R$ 230,00 < R$ 20.000,00 => ISENTO de IR sobre lucro
```

**Exemplo 2 - Tributado:**
```
Cliente B - Rebalanceamento em marco (grande investidor):
  Venda: 500 BBDC4 x R$ 16,00 = R$ 8.000,00 (PM: R$ 14,00)
  Venda: 300 WEGE3 x R$ 45,00 = R$ 13.500,00 (PM: R$ 38,00)
  Total vendas no mes: R$ 21.500,00

  R$ 21.500,00 > R$ 20.000,00 => TRIBUTADO

  Lucro BBDC4: 500 x (16,00 - 14,00) = R$ 1.000,00
  Lucro WEGE3: 300 x (45,00 - 38,00) = R$ 2.100,00
  Lucro total: R$ 3.100,00

  IR devido: R$ 3.100,00 x 20% = R$ 620,00

  Publicar no Kafka: IR_VENDA, ClienteB, R$ 620,00
```

**Exemplo 3 - Vendas acima de R$ 20k mas com prejuizo:**
```
Cliente C - Rebalanceamento:
  Venda: 400 PETR4 x R$ 32,00 = R$ 12.800,00 (PM: R$ 35,00)
  Venda: 200 VALE3 x R$ 58,00 = R$ 11.600,00 (PM: R$ 55,00)
  Total vendas no mes: R$ 24.400,00

  R$ 24.400,00 > R$ 20.000,00 => TRIBUTADO (verifica lucro)

  Lucro PETR4: 400 x (32,00 - 35,00) = -R$ 1.200,00 (PREJUIZO)
  Lucro VALE3: 200 x (58,00 - 55,00) = R$ 600,00 (LUCRO)
  Lucro liquido total: -R$ 1.200 + R$ 600 = -R$ 600,00 (PREJUIZO)

  IR devido: R$ 0,00 (nao ha lucro liquido para tributar)
```

---

## 7. Regras da Tela de Rentabilidade

### 7.1. Informacoes Obrigatorias

| Regra | Descricao |
|---|---|
| RN-063 | Exibir **saldo total** da carteira (soma de Qtd x Cotacao Atual de cada ativo) |
| RN-064 | Exibir **P/L por ativo**: (Cotacao Atual - Preco Medio) x Quantidade |
| RN-065 | Exibir **P/L total**: soma de todos os P/L individuais |
| RN-066 | Exibir **rentabilidade percentual**: ((Valor Atual - Valor Investido) / Valor Investido) x 100 |
| RN-067 | Exibir o **preco medio** de cada ativo |
| RN-068 | Exibir a **quantidade** de cada ativo em custodia |
| RN-069 | Exibir a **cotacao atual** de cada ativo |
| RN-070 | Exibir a **composicao percentual** real da carteira (quanto cada ativo representa do total) |

**Exemplo - Tela de Rentabilidade do Cliente A:**
```
+========================================================+
| CARTEIRA DO CLIENTE A                                   |
| Valor Investido Total: R$ 6.000,00                      |
| Valor Atual Total:     R$ 6.450,00                      |
| P/L Total:             +R$ 450,00                       |
| Rentabilidade:         +7,50%                           |
+========================================================+
| Ativo  | Qtd | PM     | Cotacao | Valor   | P/L    | % |
|--------|-----|--------|---------|---------|--------|---|
| PETR4  |  24 | 35,50  |  37,00  |  888,00 |+36,00  |14%|
| VALE3  |  12 | 60,00  |  65,00  |  780,00 |+60,00  |12%|
| ITUB4  |  18 | 29,00  |  31,00  |  558,00 |+36,00  | 9%|
| BBDC4  |  30 | 14,50  |  15,50  |  465,00 |+30,00  | 7%|
| WEGE3  |   6 | 38,00  |  42,00  |  252,00 |+24,00  | 4%|
+========================================================+
```

---

## 8. Mensagem Kafka - Formato Sugerido

### 8.1. IR Dedo-Duro (Compra)

```json
{
  "tipo": "IR_DEDO_DURO",
  "clienteId": 1,
  "cpf": "12345678901",
  "ticker": "PETR4",
  "tipoOperacao": "COMPRA",
  "quantidade": 8,
  "precoUnitario": 35.00,
  "valorOperacao": 280.00,
  "aliquota": 0.00005,
  "valorIR": 0.01,
  "dataOperacao": "2026-02-05T10:00:00Z"
}
```

### 8.2. IR Venda (Rebalanceamento)

```json
{
  "tipo": "IR_VENDA",
  "clienteId": 2,
  "cpf": "98765432109",
  "mesReferencia": "2026-03",
  "totalVendasMes": 21500.00,
  "lucroLiquido": 3100.00,
  "aliquota": 0.20,
  "valorIR": 620.00,
  "detalhes": [
    {
      "ticker": "BBDC4",
      "quantidade": 500,
      "precoVenda": 16.00,
      "precoMedio": 14.00,
      "lucro": 1000.00
    },
    {
      "ticker": "WEGE3",
      "quantidade": 300,
      "precoVenda": 45.00,
      "precoMedio": 38.00,
      "lucro": 2100.00
    }
  ],
  "dataCalculo": "2026-03-15T10:00:00Z"
}
```

---

## 9. Resumo de Regras por Prioridade

### Criticas (devem funcionar corretamente):
- Motor de compra nos dias 5/15/25
- Distribuicao proporcional para filhotes
- Calculo de preco medio
- Residuos na conta master
- Publicacao Kafka do IR dedo-duro

### Importantes (impactam a avaliacao):
- Rebalanceamento por mudanca de cesta
- Regra dos R$ 20.000 de IR
- Separacao lote padrao vs fracionario
- Tela de rentabilidade

### Desejaveis (diferenciais):
- Rebalanceamento por desvio de proporcao
- Historico completo de operacoes
- Compensacao de prejuizo entre meses

---

*Este documento e parte do material de apoio do Desafio Tecnico de Compra Programada de Acoes da Itau Corretora.*
