# Cabe na Semana

Aplicação web local para estudantes descobrirem se tarefas, provas e compromissos realmente cabem na capacidade disponível da semana. O produto combina prazo, importância e esforço em uma prioridade explicável, destaca sobrecarga e mantém o plano revisável em um Kanban.

## Pré-requisitos

- .NET SDK 10.0.301 ou outro patch compatível da linha 10.0. O arquivo `global.json` fixa a linha instalada e permite avanço para o patch mais recente.
- macOS, Linux ou Windows.
- Docker Desktop ou Docker Engine com Compose, apenas para a execução conteinerizada com PostgreSQL.

O projeto usa .NET 10, uma versão LTS ativa. A execução direta não exige Node.js, Docker, conta externa ou serviço de banco de dados.

## Executar localmente

Na raiz do projeto:

```bash
dotnet restore CabeNaSemana.slnx
dotnet run --project src/CabeNaSemana.Web/CabeNaSemana.Web.csproj
```

Abra [http://localhost:5147](http://localhost:5147). O perfil de desenvolvimento também solicita a abertura automática do navegador.

Na primeira execução, a aplicação cria `src/CabeNaSemana.Web/Data/cabe-na-semana.db` e inclui cinco atividades fictícias. O arquivo é local e está ignorado no Git.

## Executar com Docker e PostgreSQL

Crie o arquivo local de variáveis e troque a senha de exemplo:

```bash
cp .env.example .env
```

Depois, abra `.env`, defina uma senha local forte em `POSTGRES_PASSWORD` e execute:

```bash
docker compose up --build -d
```

Acesse [http://localhost:8080](http://localhost:8080). O Compose inicia dois serviços:

- `app`: a aplicação ASP.NET Core MVC, executada como usuário não-root;
- `postgres`: PostgreSQL 17.11, com dados persistidos no volume `postgres_data`.

Verifique o estado e acompanhe os logs:

```bash
docker compose ps
docker compose logs -f app
```

Abra o PostgreSQL pelo terminal:

```bash
docker compose exec postgres psql -U cabe_app -d cabe_na_semana
```

Dentro do `psql`, liste e consulte as tarefas:

```sql
\dt
SELECT * FROM "StudyTasks";
\q
```

As credenciais, o banco e as portas são configuráveis em `.env`. A porta do PostgreSQL é publicada somente em `127.0.0.1:5432`, permitindo também o uso de um cliente gráfico local.

Pare os containers sem apagar os dados:

```bash
docker compose down
```

Para apagar também o banco persistido, use `docker compose down -v`. Esse último comando remove definitivamente os volumes do projeto.

## Testar

Suíte completa:

```bash
dotnet test CabeNaSemana.slnx
```

Cobertura das regras de domínio e aplicação:

```bash
dotnet test CabeNaSemana.slnx \
  --collect:"XPlat Code Coverage" \
  --results-directory work/TestResults \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Include='[CabeNaSemana.Domain]*,[CabeNaSemana.Application]*'
```

Auditoria de dependências vulneráveis:

```bash
dotnet list CabeNaSemana.slnx package --vulnerable --include-transitive
```

## Como usar

1. Ajuste a capacidade semanal em horas.
2. Cadastre uma atividade com título, prazo, importância, esforço e coluna inicial.
3. Leia a prioridade e a explicação em cada cartão.
4. Use **Mover** para alterar a etapa pelo teclado, mouse ou toque.
5. Edite estimativas conforme aprende mais sobre a atividade; a capacidade é recalculada.

Atividades em **Esta semana** e **Em andamento** consomem capacidade. **A planejar** ainda não entra na conta, e **Concluído** deixa de disputar horas.

## Regra de planejamento

A pontuação é determinística e fica em `CabeNaSemana.Domain`:

- prazo: atrasada `+50`, hoje `+46`, até 2 dias `+40`, até 7 dias `+26`, até 14 dias `+14`, depois `+5`;
- importância: crítica `+38`, alta `+28`, média `+18`, baixa `+8`;
- esforço: até 2 h `+5`, até 5 h `+3`, até 8 h `0`, acima de 8 h `-3`.

Faixas: urgente a partir de 65, alta a partir de 45, média a partir de 25 e baixa abaixo disso. A capacidade é alocada por prioridade; atividades que ultrapassam o limite recebem **Fora da capacidade**.

## Arquitetura

```text
src/
├── CabeNaSemana.Domain/          entidades e regras puras
├── CabeNaSemana.Application/     casos de uso, contratos e DTOs
├── CabeNaSemana.Infrastructure/  EF Core, SQLite/PostgreSQL e dados de demonstração
└── CabeNaSemana.Web/             ASP.NET Core MVC, Razor, CSS e JS mínimo
tests/
└── CabeNaSemana.Tests/           domínio, aplicação, SQLite e MVC
```

Dependências apontam para dentro: a aplicação depende do domínio; infraestrutura implementa contratos da aplicação; MVC compõe tudo por injeção de dependência. A regra central não conhece HTTP, Razor, EF Core, SQLite ou PostgreSQL.

O provider é selecionado por `Database:Provider`. A configuração local usa `Sqlite`; o Compose injeta `PostgreSql` e a connection string do serviço `postgres`. O Npgsql usa retry transitório para tolerar o período de inicialização do banco.

O MVC usa `BoardController`, Razor Views e o padrão Post/Redirect/Get nas operações válidas. Todos os POSTs usam antiforgery automático. Erros de validação preservam os valores e direcionam o foco para o primeiro campo inválido.

## Decisões de interface

- Direção visual de “mesa de estudos”: papel quadriculado, azul-tinta, coral de alerta e lima para capacidade.
- Kanban em quatro colunas no desktop e uma coluna legível no mobile.
- Movimentação por menu de botões nativos em vez de drag-and-drop, atendendo teclado, toque e WCAG 2.2 sem manter duas interações concorrentes.
- Alertas usam texto, ícone e cor; foco é visível; alvos têm pelo menos 44 px; movimento reduzido é respeitado.

## Limitações reais do MVP

- Uso local e individual; não há autenticação, sincronização ou colaboração.
- O banco é criado com `EnsureCreated`, tanto no SQLite quanto no PostgreSQL. Isso é adequado ao MVP; evolução de esquema deve adotar migrations antes de uma distribuição com dados duráveis.
- Não há calendário, recorrência, subtarefas nem divisão automática do esforço por dia.
- A prioridade é uma heurística transparente, não uma garantia de conclusão.
- O quadro não implementa drag-and-drop; a alternativa acessível por menu é a interação oficial.

## Segurança e privacidade

Não há integrações externas nem dados sensíveis nos exemplos. Entradas são validadas no MVC e no domínio, comandos de banco passam pelo EF Core, e POSTs têm proteção antiforgery. O bundle nativo `SQLitePCLRaw.bundle_e_sqlite3` está fixado em `3.0.5` para evitar a linha transitiva afetada pelo advisory `GHSA-2m69-gcr7-jv3q`.

No Compose, a senha vem de `.env`, que está ignorado no Git; o PostgreSQL só é publicado na interface local; a aplicação roda sem root, sem capabilities Linux e com `no-new-privileges`. As chaves de proteção de dados do ASP.NET ficam no volume `data_protection_keys`. Esse Compose é voltado ao uso local, não substitui orquestração, TLS e gestão de segredos de produção.
