# Sistema de Compra Programada de Ações — Itaú Corretora

Sistema de investimento recorrente automatizado em uma carteira recomendada de 5 ações (**Top Five**), com compras consolidadas na conta master e distribuição proporcional para as custódias individuais de cada cliente.

---

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Docker](https://www.docker.com/) e Docker Compose

---

## Como rodar

### 1. Subir a infraestrutura (MySQL + Kafka)

```bash
docker compose up -d
```

### 2. Aplicar as migrations do banco

```bash
cd src/Infrastructure.Data
dotnet ef database update --startup-project ../Api
```

### 3. Importar arquivo COTAHIST (cotações)

Coloque o arquivo TXT da B3 na pasta `cotacoes/` na raiz do projeto:

```
cotacoes/COTAHIST_D20260225.TXT
```

### 4. Rodar a API

```bash
cd src/Api
dotnet run
```

A API estará disponível em `https://localhost:5001` com Swagger em `https://localhost:5001/` (rota raiz).

---

## Endpoints

### Clientes

| Método | Rota                               | Descrição               |
| ------ | ---------------------------------- | ----------------------- |
| POST   | `/api/clientes/adesao`             | Aderir ao produto       |
| POST   | `/api/clientes/{id}/saida`         | Sair do produto         |
| PUT    | `/api/clientes/{id}/valor-mensal`  | Alterar aporte mensal   |
| GET    | `/api/clientes/{id}/carteira`      | Consultar carteira      |
| GET    | `/api/clientes/{id}/rentabilidade` | Consultar rentabilidade |

### Administração

| Método | Rota                         | Descrição                        |
| ------ | ---------------------------- | -------------------------------- |
| POST   | `/api/admin/cesta`           | Cadastrar/Alterar cesta Top Five |
| GET    | `/api/admin/cesta/atual`     | Visualizar cesta vigente         |
| GET    | `/api/admin/cesta/historico` | Histórico de cestas              |

### Debug (testes manuais)

| Método | Rota                                              | Descrição                           |
| ------ | ------------------------------------------------- | ----------------------------------- |
| POST   | `/api/debug/execute-purchase?installmentStr=Day5` | Executar compra manualmente         |
| POST   | `/api/debug/rebalance-deviation`                  | Executar rebalanceamento por desvio |

---

## Serviços do Docker

| Serviço  | Porta | Descrição                                  |
| -------- | ----- | ------------------------------------------ |
| MySQL    | 3306  | Banco de dados principal                   |
| Kafka    | 9092  | Mensageria para eventos de IR              |
| Kafka UI | 8080  | Interface web para monitorar tópicos Kafka |

---

## Arquitetura

O projeto segue **Clean Architecture** com 4 camadas:

```
Domain          → Entidades, interfaces de repositório, eventos de domínio
Application     → Serviços de aplicação, casos de uso, DTOs
Infrastructure  → Repositórios (MySQL/EF), Kafka, COTAHIST parser, Event Store
Api             → Endpoints REST, Background Services, Swagger
```

### Padrões implementados

- **Event Sourcing**: cada compra e venda gera um `DomainEvent` persistido no `EventStore` (tabela MySQL), permitindo reconstruir o estado do aggregate a qualquer momento
- **CQRS (simplificado)**: leitura via read models (`CustomerCustodyItem`), escrita via aggregates
- **Repository Pattern**: todas as entidades acessam o banco através de interfaces
- **Background Services**: os motores de compra e rebalanceamento rodam automaticamente via `IHostedService`

### Fluxo do motor de compra (dias 5, 15 e 25)

1. Coleta todos os clientes ativos e calcula 1/3 do aporte mensal de cada um
2. Soma os valores e calcula a quantidade de cada ativo (TRUNCAR) usando a cotação de fechamento do COTAHIST
3. Desconta o saldo remanescente da custódia master
4. Registra a compra separando lote padrão (múltiplos de 100) e fracionário
5. Distribui proporcionalmente para cada cliente (TRUNCAR)
6. Residuos ficam na custódia master para o próximo ciclo
7. Publica evento de IR dedo-duro (0,005%) no Kafka para cada distribuição

### Kafka — Tópicos

| Tópico           | Evento                                                       |
| ---------------- | ------------------------------------------------------------ |
| `ir-dedo-duro`   | IR retido na fonte a cada distribuição de compra             |
| `ir-rebalancing` | IR sobre lucro em vendas de rebalanceamento (se > R$20k/mês) |

---

## Testes

```bash
dotnet test tests/UnitTests
```

Cobertura dos testes unitários:

- `IrCalculationService` — IR dedo-duro e IR sobre vendas
- `CustomerCustodyAggregate` — preço médio, registro de compras e vendas, event sourcing
- `CotahistParser` — parse do layout fixo da B3
- `CustomerService` — adesão, saída, alteração de valor
- `BasketService` — criação, validações, disparo de rebalanceamento

---

## Decisões Técnicas

### Por que Event Sourcing?

O histórico de todas as operações de um cliente (compras, vendas, rebalanceamentos) é preservado como sequência imutável de eventos. Isso permite auditar qualquer operação, recalcular posições a qualquer ponto no tempo e gerar relatórios históricos precisos — requisitos naturais de um sistema financeiro.

### Por que MySQL?

Stack obrigatória do desafio. Utilizado com Entity Framework Core e `Pomelo.EntityFrameworkCore.MySql`.

### Por que separar `CustomerCustodyItem` do Event Store?

O Event Store é a fonte de verdade (write side). O `CustomerCustodyItem` é um read model atualizado a cada evento, permitindo consultas rápidas de carteira sem precisar reproduzir todos os eventos a cada requisição.

### Por que CI?

Em um sistema financeiro, a confiabilidade das regras de negócio é crítica — um erro no cálculo de IR, no arredondamento de lotes ou na distribuição proporcional pode gerar prejuízo real aos clientes. A pipeline de CI garante que nenhuma alteração quebre o build ou os testes antes de entrar na branch principal.

### Decisão: apenas CI, sem CD automático

Optei por implementar somente a etapa de **integração contínua** (build + testes unitários). O deploy contínuo (CD) foi descartado por enquanto porque:

- A aplicação depende de infraestrutura local (MySQL + Kafka) que não está provisionada em nenhum ambiente de cloud
- O projeto é um desafio técnico, sem necessidade de ambiente de produção ativo

A pipeline roda automaticamente a cada `push` em qualquer branch e em Pull Requests com destino a `main`, garantindo:

1. `dotnet restore` — resolução de dependências
2. `dotnet build --configuration Release` — compilação sem erros
3. `dotnet test` — execução dos testes unitários com coleta de code coverage
4. Upload dos resultados (TRX + coverage) como artefato da run
