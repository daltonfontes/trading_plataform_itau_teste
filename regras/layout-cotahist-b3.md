# Layout do Arquivo COTAHIST da B3

## Guia de Referencia para Parse do Arquivo de Cotacoes Historicas

---

## 1. Visao Geral

O arquivo COTAHIST e disponibilizado pela B3 diariamente e contem as cotacoes historicas de todos os ativos negociados no pregao. O formato e um arquivo texto (TXT) de **campos com tamanho fixo** (layout posicional), onde cada linha tem exatamente **245 caracteres**.

### Como Obter

1. Acesse: [https://www.b3.com.br/pt_br/market-data-e-indices/servicos-de-dados/market-data/historico/mercado-a-vista/cotacoes-historicas/](https://www.b3.com.br/pt_br/market-data-e-indices/servicos-de-dados/market-data/historico/mercado-a-vista/cotacoes-historicas/)
2. Selecione **"Arquivo Diario"** e a data desejada
3. Faca o download do arquivo `.TXT` (ou `.ZIP` contendo o `.TXT`)
4. Salve na pasta `cotacoes/` do projeto com o nome original (ex: `COTAHIST_D20260225.TXT`)

---

## 2. Estrutura do Arquivo

O arquivo possui **3 tipos de registro**:

| Tipo | Codigo (TIPREG) | Descricao |
|---|---|---|
| Header | `00` | Primeira linha do arquivo - informacoes do arquivo |
| Detalhe | `01` | Linhas de cotacao - uma por ativo/pregao |
| Trailer | `99` | Ultima linha - total de registros |

**Voce deve processar apenas os registros de tipo `01` (detalhe).**

---

## 3. Layout do Registro de Detalhe (TIPREG = 01)

Cada linha de detalhe contem os seguintes campos:

| Campo | Posicao Inicio | Posicao Fim | Tamanho | Tipo | Descricao |
|---|---|---|---|---|---|
| TIPREG | 1 | 2 | 2 | N | Tipo de registro (sempre "01") |
| DATPRE | 3 | 10 | 8 | N | Data do pregao (AAAAMMDD) |
| CODBDI | 11 | 12 | 2 | A | Codigo BDI (ver tabela abaixo) |
| CODNEG | 13 | 24 | 12 | A | **Codigo de negociacao (TICKER)** |
| TPMERC | 25 | 27 | 3 | N | Tipo de mercado (ver tabela abaixo) |
| NOMRES | 28 | 39 | 12 | A | Nome resumido da empresa |
| ESPECI | 40 | 49 | 10 | A | Especificacao do papel |
| PRAZOT | 50 | 52 | 3 | A | Prazo em dias do mercado a termo |
| MODREF | 53 | 56 | 4 | A | Moeda de referencia |
| PREABE | 57 | 69 | 13 | N(11,2) | **Preco de abertura** |
| PREMAX | 70 | 82 | 13 | N(11,2) | **Preco maximo** |
| PREMIN | 83 | 95 | 13 | N(11,2) | **Preco minimo** |
| PREMED | 96 | 108 | 13 | N(11,2) | Preco medio |
| PREULT | 109 | 121 | 13 | N(11,2) | **Preco do ultimo negocio (FECHAMENTO)** |
| PREOFC | 122 | 134 | 13 | N(11,2) | Preco da melhor oferta de compra |
| PREOFV | 135 | 147 | 13 | N(11,2) | Preco da melhor oferta de venda |
| TOTNEG | 148 | 152 | 5 | N | Numero de negocios efetuados |
| QUATOT | 153 | 170 | 18 | N | Quantidade total de titulos negociados |
| VOLTOT | 171 | 188 | 18 | N(16,2) | Volume total de titulos negociados |
| PREEXE | 189 | 201 | 13 | N(11,2) | Preco de exercicio (opcoes) |
| INDOPC | 202 | 202 | 1 | N | Indicador de correcao de precos |
| DATVEN | 203 | 210 | 8 | N | Data do vencimento (opcoes) |
| FATCOT | 211 | 217 | 7 | N | Fator de cotacao |
| PTOEXE | 218 | 230 | 13 | N(11,2) | Preco de exercicio em pontos |
| CODISI | 231 | 242 | 12 | A | Codigo ISIN do papel |
| DISMES | 243 | 245 | 3 | N | Numero de distribuicao do papel |

### Campos Mais Importantes para o Sistema:

- **CODNEG (13-24):** Ticker do ativo (ex: `PETR4`, `VALE3`, `PETR4F`)
- **DATPRE (3-10):** Data do pregao
- **PREULT (109-121):** Preco de fechamento (cotacao a ser usada)
- **PREABE (57-69):** Preco de abertura
- **PREMAX (70-82):** Preco maximo
- **PREMIN (83-95):** Preco minimo
- **TPMERC (25-27):** Tipo de mercado (010=Vista, 020=Fracionario)
- **CODBDI (11-12):** Codigo BDI (02=Lote Padrao, 12=Fundos Imobiliarios, etc.)

---

## 4. Tabelas de Referencia

### Codigos BDI Relevantes

| Codigo | Descricao |
|---|---|
| 02 | Lote padrao |
| 05 | Sancionadas pelos regulamentos BMFBOVESPA |
| 06 | Concordatarias |
| 07 | Recuperacao extrajudicial |
| 08 | Recuperacao judicial |
| 10 | Direitos e recibos |
| 12 | Fundos imobiliarios |
| 96 | Fracionario |

**Para o sistema, filtre por CODBDI `02` (lote padrao) e `96` (fracionario).**

### Tipos de Mercado

| Codigo | Descricao |
|---|---|
| 010 | Mercado a Vista |
| 012 | Exercicio de opcoes de compra |
| 013 | Exercicio de opcoes de venda |
| 017 | Leilao |
| 020 | Fracionario |
| 030 | Termo |
| 050 | Futuro com retencao de ganhos |
| 060 | Futuro com movimentacao continua |
| 070 | Opcoes de compra |
| 080 | Opcoes de venda |

**Para o sistema, filtre por TPMERC `010` (Vista) e `020` (Fracionario).**

---

## 5. Tratamento de Precos

**ATENCAO:** Os precos no arquivo COTAHIST estao em formato inteiro com **2 casas decimais implicitas**. Ou seja, um valor de `0000000003850` no campo PREULT significa **R$ 38,50**.

**Formula de conversao:**
```
Preco Real = Valor do Campo / 100
```

**Exemplos:**
- `0000000003850` => R$ 38,50
- `0000000012345` => R$ 123,45
- `0000000000150` => R$ 1,50

---

## 6. Exemplo de Linha do Arquivo

```
01202602250200PETR4       010PETROBRAS   PN      N1   R$  0000000003520000000003650000000003480000000003560000000003580000000003570000000003590034561000000000150000000000000005376000000000000000000000000000000000000000BRPETRACNPR6180
```

### Parse da linha acima:

| Campo | Posicoes | Valor Bruto | Valor Interpretado |
|---|---|---|---|
| TIPREG | 1-2 | `01` | Registro de detalhe |
| DATPRE | 3-10 | `20260225` | 25/02/2026 |
| CODBDI | 11-12 | `02` | Lote padrao |
| CODNEG | 13-24 | `PETR4       ` | PETR4 (trim) |
| TPMERC | 25-27 | `010` | Mercado a Vista |
| NOMRES | 28-39 | `PETROBRAS   ` | PETROBRAS |
| ESPECI | 40-49 | `PN      N1` | Preferencial N1 |
| PREABE | 57-69 | `0000000003520` | R$ 35,20 |
| PREMAX | 70-82 | `0000000003650` | R$ 36,50 |
| PREMIN | 83-95 | `0000000003480` | R$ 34,80 |
| PREULT | 109-121 | `0000000003580` | **R$ 35,80** (fechamento) |

---

## 7. Exemplo de Codigo em C# (.NET Core)

```csharp
public class CotacaoB3
{
    public DateTime DataPregao { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string CodigoBDI { get; set; } = string.Empty;
    public int TipoMercado { get; set; }
    public string NomeEmpresa { get; set; } = string.Empty;
    public decimal PrecoAbertura { get; set; }
    public decimal PrecoMaximo { get; set; }
    public decimal PrecoMinimo { get; set; }
    public decimal PrecoFechamento { get; set; }
    public decimal PrecoMedio { get; set; }
    public long QuantidadeNegociada { get; set; }
    public decimal VolumeNegociado { get; set; }
}

public class CotahistParser
{
    /// <summary>
    /// Le e faz parse de um arquivo COTAHIST da B3.
    /// Retorna apenas registros de detalhe (TIPREG = 01)
    /// filtrados por mercado a vista (010) e fracionario (020).
    /// </summary>
    public IEnumerable<CotacaoB3> ParseArquivo(string caminhoArquivo)
    {
        var cotacoes = new List<CotacaoB3>();
        
        foreach (var linha in File.ReadLines(caminhoArquivo))
        {
            // Ignorar header (00) e trailer (99)
            if (linha.Length < 245)
                continue;
                
            var tipoRegistro = linha.Substring(0, 2);
            if (tipoRegistro != "01")
                continue;

            var tipoMercado = int.Parse(linha.Substring(24, 3).Trim());
            
            // Filtrar apenas mercado a vista (010) e fracionario (020)
            if (tipoMercado != 10 && tipoMercado != 20)
                continue;

            var cotacao = new CotacaoB3
            {
                DataPregao = DateTime.ParseExact(
                    linha.Substring(2, 8), "yyyyMMdd", 
                    System.Globalization.CultureInfo.InvariantCulture),
                CodigoBDI = linha.Substring(10, 2).Trim(),
                Ticker = linha.Substring(12, 12).Trim(),
                TipoMercado = tipoMercado,
                NomeEmpresa = linha.Substring(27, 12).Trim(),
                PrecoAbertura = ParsePreco(linha.Substring(56, 13)),
                PrecoMaximo = ParsePreco(linha.Substring(69, 13)),
                PrecoMinimo = ParsePreco(linha.Substring(82, 13)),
                PrecoMedio = ParsePreco(linha.Substring(95, 13)),
                PrecoFechamento = ParsePreco(linha.Substring(108, 13)),
                QuantidadeNegociada = long.Parse(linha.Substring(152, 18).Trim()),
                VolumeNegociado = ParsePreco(linha.Substring(170, 18))
            };

            cotacoes.Add(cotacao);
        }

        return cotacoes;
    }

    /// <summary>
    /// Converte o valor inteiro do arquivo para decimal com 2 casas.
    /// Ex: "0000000003850" => 38.50m
    /// </summary>
    private decimal ParsePreco(string valorBruto)
    {
        if (long.TryParse(valorBruto.Trim(), out var valor))
            return valor / 100m;
        return 0m;
    }

    /// <summary>
    /// Obtem a cotacao de fechamento mais recente de um ticker especifico.
    /// Busca na pasta cotacoes/ o arquivo mais recente.
    /// </summary>
    public CotacaoB3? ObterCotacaoFechamento(string pastaCotacoes, string ticker)
    {
        var arquivos = Directory.GetFiles(pastaCotacoes, "COTAHIST_D*.TXT")
            .OrderByDescending(f => f)
            .ToList();

        foreach (var arquivo in arquivos)
        {
            var cotacoes = ParseArquivo(arquivo);
            var cotacao = cotacoes
                .Where(c => c.Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase))
                .Where(c => c.TipoMercado == 10) // Mercado a vista
                .FirstOrDefault();

            if (cotacao != null)
                return cotacao;
        }

        return null;
    }
}
```

### Exemplo de uso:

```csharp
var parser = new CotahistParser();

// Parse de um arquivo completo
var cotacoes = parser.ParseArquivo("cotacoes/COTAHIST_D20260225.TXT");

// Filtrar apenas acoes do lote padrao
var acoes = cotacoes.Where(c => c.CodigoBDI == "02" && c.TipoMercado == 10);

// Obter cotacao de fechamento de PETR4
var petr4 = parser.ObterCotacaoFechamento("cotacoes", "PETR4");
if (petr4 != null)
{
    Console.WriteLine($"PETR4 - Fechamento: R$ {petr4.PrecoFechamento:F2}");
    // Output: PETR4 - Fechamento: R$ 35.80
}
```

---

## 8. Dicas de Implementacao

1. **Performance:** O arquivo COTAHIST diario pode ter milhares de linhas. Considere usar `File.ReadLines()` (lazy) ao inves de `File.ReadAllLines()` para economia de memoria.

2. **Cache:** Apos fazer o parse, salve as cotacoes relevantes (apenas os 5 tickers da cesta) no banco MySQL para consultas rapidas.

3. **Encoding:** O arquivo usa encoding ISO-8859-1 (Latin1). No .NET Core, use:
   ```csharp
   Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
   var encoding = Encoding.GetEncoding("ISO-8859-1");
   File.ReadLines(caminho, encoding);
   ```

4. **Validacao:** Sempre valide se o arquivo existe e se o ticker foi encontrado antes de prosseguir com as compras.

5. **Trim:** Os campos sao preenchidos com espacos a direita. Sempre faca `.Trim()` ao extrair os valores.

---

*Este documento e parte do material de apoio do Desafio Tecnico de Compra Programada de Acoes da Itau Corretora.*
