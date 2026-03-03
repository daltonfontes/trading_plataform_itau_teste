# Exemplos de Contratos de API (Request / Response)

## Sistema de Compra Programada de Acoes - Itau Corretora

Este documento apresenta exemplos dos endpoints REST esperados, com exemplos de request e response para servir de referencia aos candidatos.

---

## 1. Endpoints do Cliente

### 1.1. Aderir ao Produto

**`POST /api/clientes/adesao`**

Request:
```json
{
  "nome": "Joao da Silva",
  "cpf": "12345678901",
  "email": "joao.silva@email.com",
  "valorMensal": 3000.00
}
```

Response (201 Created):
```json
{
  "clienteId": 1,
  "nome": "Joao da Silva",
  "cpf": "12345678901",
  "email": "joao.silva@email.com",
  "valorMensal": 3000.00,
  "ativo": true,
  "dataAdesao": "2026-02-05T10:30:00Z",
  "contaGrafica": {
    "id": 1,
    "numeroConta": "FLH-000001",
    "tipo": "FILHOTE",
    "dataCriacao": "2026-02-05T10:30:00Z"
  }
}
```

Response (400 Bad Request - CPF duplicado):
```json
{
  "erro": "CPF ja cadastrado no sistema.",
  "codigo": "CLIENTE_CPF_DUPLICADO"
}
```

Response (400 Bad Request - valor invalido):
```json
{
  "erro": "O valor mensal minimo e de R$ 100,00.",
  "codigo": "VALOR_MENSAL_INVALIDO"
}
```

---

### 1.2. Sair do Produto

**`POST /api/clientes/{clienteId}/saida`**

Response (200 OK):
```json
{
  "clienteId": 1,
  "nome": "Joao da Silva",
  "ativo": false,
  "dataSaida": "2026-03-10T14:00:00Z",
  "mensagem": "Adesao encerrada. Sua posicao em custodia foi mantida."
}
```

Response (404 Not Found):
```json
{
  "erro": "Cliente nao encontrado.",
  "codigo": "CLIENTE_NAO_ENCONTRADO"
}
```

---

### 1.3. Alterar Valor Mensal

**`PUT /api/clientes/{clienteId}/valor-mensal`**

Request:
```json
{
  "novoValorMensal": 6000.00
}
```

Response (200 OK):
```json
{
  "clienteId": 1,
  "valorMensalAnterior": 3000.00,
  "valorMensalNovo": 6000.00,
  "dataAlteracao": "2026-02-10T09:00:00Z",
  "mensagem": "Valor mensal atualizado. O novo valor sera considerado a partir da proxima data de compra."
}
```

---

### 1.4. Consultar Carteira (Custodia)

**`GET /api/clientes/{clienteId}/carteira`**

Response (200 OK):
```json
{
  "clienteId": 1,
  "nome": "Joao da Silva",
  "contaGrafica": "FLH-000001",
  "dataConsulta": "2026-02-25T15:00:00Z",
  "resumo": {
    "valorTotalInvestido": 6000.00,
    "valorAtualCarteira": 6450.00,
    "plTotal": 450.00,
    "rentabilidadePercentual": 7.50
  },
  "ativos": [
    {
      "ticker": "PETR4",
      "quantidade": 24,
      "precoMedio": 35.50,
      "cotacaoAtual": 37.00,
      "valorAtual": 888.00,
      "pl": 36.00,
      "plPercentual": 4.23,
      "composicaoCarteira": 13.77
    },
    {
      "ticker": "VALE3",
      "quantidade": 12,
      "precoMedio": 60.00,
      "cotacaoAtual": 65.00,
      "valorAtual": 780.00,
      "pl": 60.00,
      "plPercentual": 8.33,
      "composicaoCarteira": 12.09
    },
    {
      "ticker": "ITUB4",
      "quantidade": 18,
      "precoMedio": 29.00,
      "cotacaoAtual": 31.00,
      "valorAtual": 558.00,
      "pl": 36.00,
      "plPercentual": 6.90,
      "composicaoCarteira": 8.65
    },
    {
      "ticker": "BBDC4",
      "quantidade": 30,
      "precoMedio": 14.50,
      "cotacaoAtual": 15.50,
      "valorAtual": 465.00,
      "pl": 30.00,
      "plPercentual": 6.90,
      "composicaoCarteira": 7.21
    },
    {
      "ticker": "WEGE3",
      "quantidade": 6,
      "precoMedio": 38.00,
      "cotacaoAtual": 42.00,
      "valorAtual": 252.00,
      "pl": 24.00,
      "plPercentual": 10.53,
      "composicaoCarteira": 3.91
    }
  ]
}
```

---

### 1.5. Consultar Rentabilidade Detalhada

**`GET /api/clientes/{clienteId}/rentabilidade`**

Response (200 OK):
```json
{
  "clienteId": 1,
  "nome": "Joao da Silva",
  "dataConsulta": "2026-02-25T15:00:00Z",
  "rentabilidade": {
    "valorTotalInvestido": 6000.00,
    "valorAtualCarteira": 6450.00,
    "plTotal": 450.00,
    "rentabilidadePercentual": 7.50
  },
  "historicoAportes": [
    {
      "data": "2026-01-05",
      "valor": 1000.00,
      "parcela": "1/3"
    },
    {
      "data": "2026-01-15",
      "valor": 1000.00,
      "parcela": "2/3"
    },
    {
      "data": "2026-01-27",
      "valor": 1000.00,
      "parcela": "3/3"
    },
    {
      "data": "2026-02-05",
      "valor": 1000.00,
      "parcela": "1/3"
    },
    {
      "data": "2026-02-17",
      "valor": 1000.00,
      "parcela": "2/3"
    },
    {
      "data": "2026-02-25",
      "valor": 1000.00,
      "parcela": "3/3"
    }
  ],
  "evolucaoCarteira": [
    {
      "data": "2026-01-05",
      "valorCarteira": 990.00,
      "valorInvestido": 1000.00,
      "rentabilidade": -1.00
    },
    {
      "data": "2026-01-15",
      "valorCarteira": 2050.00,
      "valorInvestido": 2000.00,
      "rentabilidade": 2.50
    },
    {
      "data": "2026-01-27",
      "valorCarteira": 3150.00,
      "valorInvestido": 3000.00,
      "rentabilidade": 5.00
    },
    {
      "data": "2026-02-05",
      "valorCarteira": 4200.00,
      "valorInvestido": 4000.00,
      "rentabilidade": 5.00
    },
    {
      "data": "2026-02-17",
      "valorCarteira": 5350.00,
      "valorInvestido": 5000.00,
      "rentabilidade": 7.00
    },
    {
      "data": "2026-02-25",
      "valorCarteira": 6450.00,
      "valorInvestido": 6000.00,
      "rentabilidade": 7.50
    }
  ]
}
```

---

## 2. Endpoints Administrativos

### 2.1. Cadastrar / Alterar Cesta Top Five

**`POST /api/admin/cesta`**

Request:
```json
{
  "nome": "Top Five - Fevereiro 2026",
  "itens": [
    { "ticker": "PETR4", "percentual": 30.00 },
    { "ticker": "VALE3", "percentual": 25.00 },
    { "ticker": "ITUB4", "percentual": 20.00 },
    { "ticker": "BBDC4", "percentual": 15.00 },
    { "ticker": "WEGE3", "percentual": 10.00 }
  ]
}
```

Response (201 Created):
```json
{
  "cestaId": 1,
  "nome": "Top Five - Fevereiro 2026",
  "ativa": true,
  "dataCriacao": "2026-02-01T09:00:00Z",
  "itens": [
    { "ticker": "PETR4", "percentual": 30.00 },
    { "ticker": "VALE3", "percentual": 25.00 },
    { "ticker": "ITUB4", "percentual": 20.00 },
    { "ticker": "BBDC4", "percentual": 15.00 },
    { "ticker": "WEGE3", "percentual": 10.00 }
  ],
  "rebalanceamentoDisparado": false,
  "mensagem": "Primeira cesta cadastrada com sucesso."
}
```

Response (201 Created - com rebalanceamento):
```json
{
  "cestaId": 2,
  "nome": "Top Five - Marco 2026",
  "ativa": true,
  "dataCriacao": "2026-03-01T09:00:00Z",
  "itens": [
    { "ticker": "PETR4", "percentual": 25.00 },
    { "ticker": "VALE3", "percentual": 20.00 },
    { "ticker": "ITUB4", "percentual": 20.00 },
    { "ticker": "ABEV3", "percentual": 20.00 },
    { "ticker": "RENT3", "percentual": 15.00 }
  ],
  "cestaAnteriorDesativada": {
    "cestaId": 1,
    "nome": "Top Five - Fevereiro 2026",
    "dataDesativacao": "2026-03-01T09:00:00Z"
  },
  "rebalanceamentoDisparado": true,
  "ativosRemovidos": ["BBDC4", "WEGE3"],
  "ativosAdicionados": ["ABEV3", "RENT3"],
  "mensagem": "Cesta atualizada. Rebalanceamento disparado para 150 clientes ativos."
}
```

Response (400 Bad Request - percentuais invalidos):
```json
{
  "erro": "A soma dos percentuais deve ser exatamente 100%. Soma atual: 95%.",
  "codigo": "PERCENTUAIS_INVALIDOS"
}
```

Response (400 Bad Request - quantidade de ativos):
```json
{
  "erro": "A cesta deve conter exatamente 5 ativos. Quantidade informada: 4.",
  "codigo": "QUANTIDADE_ATIVOS_INVALIDA"
}
```

---

### 2.2. Consultar Cesta Atual

**`GET /api/admin/cesta/atual`**

Response (200 OK):
```json
{
  "cestaId": 2,
  "nome": "Top Five - Marco 2026",
  "ativa": true,
  "dataCriacao": "2026-03-01T09:00:00Z",
  "itens": [
    { "ticker": "PETR4", "percentual": 25.00, "cotacaoAtual": 37.00 },
    { "ticker": "VALE3", "percentual": 20.00, "cotacaoAtual": 65.00 },
    { "ticker": "ITUB4", "percentual": 20.00, "cotacaoAtual": 31.00 },
    { "ticker": "ABEV3", "percentual": 20.00, "cotacaoAtual": 14.50 },
    { "ticker": "RENT3", "percentual": 15.00, "cotacaoAtual": 49.00 }
  ]
}
```

---

### 2.3. Historico de Cestas

**`GET /api/admin/cesta/historico`**

Response (200 OK):
```json
{
  "cestas": [
    {
      "cestaId": 2,
      "nome": "Top Five - Marco 2026",
      "ativa": true,
      "dataCriacao": "2026-03-01T09:00:00Z",
      "dataDesativacao": null,
      "itens": [
        { "ticker": "PETR4", "percentual": 25.00 },
        { "ticker": "VALE3", "percentual": 20.00 },
        { "ticker": "ITUB4", "percentual": 20.00 },
        { "ticker": "ABEV3", "percentual": 20.00 },
        { "ticker": "RENT3", "percentual": 15.00 }
      ]
    },
    {
      "cestaId": 1,
      "nome": "Top Five - Fevereiro 2026",
      "ativa": false,
      "dataCriacao": "2026-02-01T09:00:00Z",
      "dataDesativacao": "2026-03-01T09:00:00Z",
      "itens": [
        { "ticker": "PETR4", "percentual": 30.00 },
        { "ticker": "VALE3", "percentual": 25.00 },
        { "ticker": "ITUB4", "percentual": 20.00 },
        { "ticker": "BBDC4", "percentual": 15.00 },
        { "ticker": "WEGE3", "percentual": 10.00 }
      ]
    }
  ]
}
```

---

## 3. Endpoints de Consulta (Conta Master)

### 3.1. Consultar Custodia Master

**`GET /api/admin/conta-master/custodia`**

Response (200 OK):
```json
{
  "contaMaster": {
    "id": 1,
    "numeroConta": "MST-000001",
    "tipo": "MASTER"
  },
  "custodia": [
    {
      "ticker": "PETR4",
      "quantidade": 1,
      "precoMedio": 35.00,
      "valorAtual": 37.00,
      "origem": "Residuo distribuicao 2026-02-05"
    },
    {
      "ticker": "ITUB4",
      "quantidade": 1,
      "precoMedio": 30.00,
      "valorAtual": 31.00,
      "origem": "Residuo distribuicao 2026-02-05"
    },
    {
      "ticker": "WEGE3",
      "quantidade": 1,
      "precoMedio": 40.00,
      "valorAtual": 42.00,
      "origem": "Residuo distribuicao 2026-02-05"
    }
  ],
  "valorTotalResiduo": 110.00
}
```

---

## 4. Endpoints do Motor de Compra (consulta/manual)

### 4.1. Executar Compra Manualmente (para testes)

**`POST /api/motor/executar-compra`**

Request:
```json
{
  "dataReferencia": "2026-02-05"
}
```

Response (200 OK):
```json
{
  "dataExecucao": "2026-02-05T10:00:00Z",
  "totalClientes": 3,
  "totalConsolidado": 3500.00,
  "ordensCompra": [
    {
      "ticker": "PETR4",
      "quantidadeTotal": 28,
      "detalhes": [
        { "tipo": "FRACIONARIO", "ticker": "PETR4F", "quantidade": 28 }
      ],
      "precoUnitario": 35.00,
      "valorTotal": 980.00
    },
    {
      "ticker": "VALE3",
      "quantidadeTotal": 14,
      "detalhes": [
        { "tipo": "FRACIONARIO", "ticker": "VALE3F", "quantidade": 14 }
      ],
      "precoUnitario": 62.00,
      "valorTotal": 868.00
    },
    {
      "ticker": "ITUB4",
      "quantidadeTotal": 22,
      "detalhes": [
        { "tipo": "FRACIONARIO", "ticker": "ITUB4F", "quantidade": 22 }
      ],
      "precoUnitario": 30.00,
      "valorTotal": 660.00
    },
    {
      "ticker": "BBDC4",
      "quantidadeTotal": 35,
      "detalhes": [
        { "tipo": "FRACIONARIO", "ticker": "BBDC4F", "quantidade": 35 }
      ],
      "precoUnitario": 15.00,
      "valorTotal": 525.00
    },
    {
      "ticker": "WEGE3",
      "quantidadeTotal": 8,
      "detalhes": [
        { "tipo": "FRACIONARIO", "ticker": "WEGE3F", "quantidade": 8 }
      ],
      "precoUnitario": 40.00,
      "valorTotal": 320.00
    }
  ],
  "distribuicoes": [
    {
      "clienteId": 1,
      "nome": "Joao da Silva",
      "valorAporte": 1000.00,
      "ativos": [
        { "ticker": "PETR4", "quantidade": 8 },
        { "ticker": "VALE3", "quantidade": 4 },
        { "ticker": "ITUB4", "quantidade": 6 },
        { "ticker": "BBDC4", "quantidade": 10 },
        { "ticker": "WEGE3", "quantidade": 2 }
      ]
    },
    {
      "clienteId": 2,
      "nome": "Maria Souza",
      "valorAporte": 2000.00,
      "ativos": [
        { "ticker": "PETR4", "quantidade": 17 },
        { "ticker": "VALE3", "quantidade": 8 },
        { "ticker": "ITUB4", "quantidade": 13 },
        { "ticker": "BBDC4", "quantidade": 20 },
        { "ticker": "WEGE3", "quantidade": 4 }
      ]
    },
    {
      "clienteId": 3,
      "nome": "Pedro Santos",
      "valorAporte": 500.00,
      "ativos": [
        { "ticker": "PETR4", "quantidade": 4 },
        { "ticker": "VALE3", "quantidade": 2 },
        { "ticker": "ITUB4", "quantidade": 3 },
        { "ticker": "BBDC4", "quantidade": 5 },
        { "ticker": "WEGE3", "quantidade": 1 }
      ]
    }
  ],
  "residuosCustMaster": [
    { "ticker": "PETR4", "quantidade": 1 },
    { "ticker": "ITUB4", "quantidade": 1 },
    { "ticker": "WEGE3", "quantidade": 1 }
  ],
  "eventosIRPublicados": 15,
  "mensagem": "Compra programada executada com sucesso para 3 clientes."
}
```

---

## 5. Codigos de Erro Padrao

| Codigo HTTP | Codigo Erro | Descricao |
|---|---|---|
| 400 | CLIENTE_CPF_DUPLICADO | CPF ja cadastrado no sistema |
| 400 | VALOR_MENSAL_INVALIDO | Valor mensal abaixo do minimo |
| 400 | PERCENTUAIS_INVALIDOS | Soma dos percentuais diferente de 100% |
| 400 | QUANTIDADE_ATIVOS_INVALIDA | Cesta nao contem exatamente 5 ativos |
| 400 | CLIENTE_JA_INATIVO | Cliente ja havia saido do produto |
| 404 | CLIENTE_NAO_ENCONTRADO | Cliente nao encontrado |
| 404 | CESTA_NAO_ENCONTRADA | Nenhuma cesta ativa encontrada |
| 404 | COTACAO_NAO_ENCONTRADA | Arquivo COTAHIST nao encontrado para a data |
| 409 | COMPRA_JA_EXECUTADA | Compra ja foi executada para esta data |
| 500 | KAFKA_INDISPONIVEL | Erro ao publicar no topico Kafka |

---

## 6. Headers Padrao

### Request:
```
Content-Type: application/json
Accept: application/json
```

### Response:
```
Content-Type: application/json; charset=utf-8
X-Request-Id: uuid-gerado-por-requisicao
```

---

*Este documento e parte do material de apoio do Desafio Tecnico de Compra Programada de Acoes da Itau Corretora.*
