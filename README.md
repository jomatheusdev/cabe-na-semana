# Cabe na Semana

MVP de planejamento semanal para responder uma pergunta que uma lista comum não responde: **as atividades realmente cabem nas horas disponíveis?**

A pessoa informa prazo, importância e esforço. A plataforma calcula uma prioridade explicável, organiza as atividades em um Kanban e mostra quando os compromissos ultrapassam a capacidade semanal. Os cartões podem ser arrastados com mouse, toque ou teclado; o menu **Mover** permanece como alternativa acessível.

> Neste projeto, MVP significa **Minimum Viable Product — Produto Mínimo Viável**. Não significa Model–View–Presenter e não descreve o framework da interface.

## Stack

- Next.js 16, React 19 e TypeScript no frontend;
- ASP.NET Core 10 na API;
- Domain e Application independentes da interface e do banco;
- Entity Framework Core com SQLite na execução direta e PostgreSQL 17 no Docker;
- Docker Compose com três serviços de longa duração (`web`, `api` e `postgres`) e um job idempotente de bootstrap;
- xUnit, Vitest, Testing Library, Playwright e Axe para testes.

## Executar tudo com Docker

Pré-requisito: Docker Desktop ou Docker Engine com Compose.

Na raiz do repositório, crie a configuração local:

```bash
cp .env.example .env
```

Abra `.env` e substitua `POSTGRES_PASSWORD` e `APP_DB_PASSWORD` por senhas locais fortes e diferentes. O primeiro usuário administra a instância; o segundo é o único usado pela API. A conexão é montada pela biblioteca Npgsql a partir de variáveis separadas, por isso caracteres como `;`, `=`, aspas e espaços não são concatenados como parâmetros. Depois execute:

Se você já possui o volume `postgres_data`, preserve o `POSTGRES_USER` usado quando ele foi criado. O PostgreSQL não recria nem renomeia automaticamente o papel administrativo de um volume existente quando essa variável muda. Para adotar outro nome, crie/renomeie o papel conscientemente ou recrie o volume sabendo que `docker compose down -v` apaga os dados.

```bash
docker compose up --build -d
```

Acesse [http://localhost:8080](http://localhost:8080).

O Compose inicia:

- `web`: Next.js standalone na porta interna 3000 e pública 8080;
- `api`: ASP.NET Core na porta interna 8080, sem publicação direta;
- `postgres-bootstrap`: job curto e idempotente que prepara o papel limitado da API e termina com código `0`;
- `postgres`: PostgreSQL na porta 5432, publicada apenas em `127.0.0.1`.

Confira a saúde e os logs:

```bash
docker compose ps
docker compose logs -f web api postgres postgres-bootstrap
```

Pare os contêineres preservando o banco:

```bash
docker compose down
```

`docker compose down -v` também remove o volume `postgres_data` e apaga os dados persistidos. Use somente quando quiser recriar o banco do zero.

## Executar diretamente para desenvolver

Pré-requisitos:

- .NET SDK 10.0.301, conforme `global.json`;
- Node.js 24 e npm 11 recomendados para reproduzir o ambiente testado.

No primeiro terminal:

```bash
dotnet restore CabeNaSemana.slnx
dotnet run --project src/CabeNaSemana.Api/CabeNaSemana.Api.csproj
```

A API abre em [http://localhost:5147](http://localhost:5147) e usa SQLite. Na primeira execução, cria `src/CabeNaSemana.Api/Data/cabe-na-semana.db`.

No segundo terminal:

```bash
cd src/CabeNaSemana.Frontend
npm ci
npm run dev
```

Abra [http://localhost:3000](http://localhost:3000). Em desenvolvimento, o Next encaminha `/api/*` para `http://localhost:5147`.

## Ver as tabelas do PostgreSQL

Com a pilha Docker em execução, entre com o papel administrativo somente para inspeção local:

```bash
docker compose exec postgres sh -lc 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"'
```

Dentro do `psql`:

```sql
\dt

SELECT
    "Title",
    "DueDate",
    "Importance",
    "EstimatedHours",
    "Status"
FROM "StudyTasks";

SELECT * FROM "PlannerSettings";

\q
```

Também é possível conectar DBeaver, DataGrip ou pgAdmin em `localhost:5432`. Para leitura e manutenção dos dados da aplicação, prefira `APP_DB_USER` e `APP_DB_PASSWORD`; reserve `POSTGRES_USER` para administração e bootstrap.

## Como usar

1. Ajuste a capacidade semanal.
2. Cadastre uma atividade com título, prazo, importância, esforço e coluna inicial.
3. Leia a pontuação e a explicação de prioridade no cartão.
4. Arraste o cartão pela alça para outra coluna ou abra **Mover**.
5. Edite estimativas e observe o backend recalcular o plano.
6. Quando houver sobrecarga, reduza, mova ou conclua compromissos.

**Esta semana** e **Em andamento** consomem capacidade. **A planejar** ainda não entra na conta, e **Concluído** deixa de disputar horas.

## Arquitetura

```text
src/
├── CabeNaSemana.Domain/          entidade e regras puras
├── CabeNaSemana.Application/     casos de uso, portas e snapshots
├── CabeNaSemana.Infrastructure/  EF Core, SQLite/PostgreSQL e inicialização
├── CabeNaSemana.Api/             controllers, DTOs e composition root
└── CabeNaSemana.Frontend/        Next.js, React, Kanban e cliente HTTP
tests/
└── CabeNaSemana.Tests/           Domain, Application, Infrastructure e API
```

Direção das dependências do backend:

```text
Api ───────────────▶ Application ─────────▶ Domain
 │                         ▲
 └────▶ Infrastructure ────┘
              └───────────────────────────▶ Domain
```

O frontend não referencia assemblies .NET. Ele conversa com a API por HTTP/JSON. `IBoardRepository` fica na Application e é implementado por `EfBoardRepository` na Infrastructure. `Program.cs` da API liga as abstrações às implementações.

Manter frontend e backend no mesmo `src` é intencional: ambos são código do mesmo produto e evoluem no mesmo release. Isso permite alterar contrato, consumidor, Compose e testes em uma mudança coerente, sem misturar as responsabilidades internas.

O guia detalhado explica cada pasta e arquivo: [docs/architecture/cabe-na-semana-mvp.md](docs/architecture/cabe-na-semana-mvp.md).

## Por que Next.js

A interface passou a exigir estado interativo, drag-and-drop, feedback imediato e reversão quando uma gravação falha. Next.js oferece React, App Router, renderização inicial no servidor, estados de carregamento/erro, build de produção e `rewrites` para a API.

`app/page.tsx` é renderizado dinamicamente e busca o quadro com `cache: "no-store"`. Depois da hidratação, `BoardApp` coordena as mutações. A regra de prioridade não é duplicada no TypeScript: após cada alteração, o frontend recarrega o snapshot calculado pelo backend.

O custo é ter duas toolchains e dois processos. No estágio atual, o monorepo e o Compose mantêm esse custo controlado. Redux, microfrontend e BFF próprio não foram adicionados porque ainda não resolvem um problema do MVP.

## Como funciona o drag-and-drop

- `TaskCard` expõe uma alça de arraste.
- `KanbanColumnView` registra cada coluna como destino.
- `DndContext` usa sensores de mouse, toque e teclado.
- `moveTaskLocally` cria um snapshot novo para feedback otimista.
- `PATCH /api/tasks/{id}/status` persiste o movimento.
- Em sucesso, o quadro é recarregado; em erro, o snapshot anterior volta.
- Uma região viva anuncia o movimento para leitores de tela.
- O menu **Mover** executa o mesmo caso de uso sem exigir o gesto.

## API sem `/v1` ou `/v2`

O MVP possui um único consumidor e nenhum contrato externo antigo que precise continuar ativo. Por isso as rotas não carregam uma versão artificial:

| Método | Rota | Resultado principal |
|---|---|---|
| `GET` | `/api/board` | Quadro calculado |
| `GET` | `/api/tasks/{id}` | Dados editáveis da atividade |
| `POST` | `/api/tasks` | `201`, ID e `Location` |
| `PUT` | `/api/tasks/{id}` | Atividade atualizada |
| `PATCH` | `/api/tasks/{id}/status` | Coluna atualizada |
| `DELETE` | `/api/tasks/{id}` | Atividade excluída |
| `PUT` | `/api/settings/weekly-capacity` | Capacidade atualizada |
| `GET` | `/health` | Vida do processo |

Versionamento deve entrar quando uma mudança incompatível precisar coexistir com consumidores que não possam migrar juntos.

## Regra de prioridade e capacidade

A pontuação é determinística:

- prazo: atrasada `+50`, hoje `+46`, até 2 dias `+40`, até 7 dias `+26`, até 14 dias `+14`, depois `+5`;
- importância: crítica `+38`, alta `+28`, média `+18`, baixa `+8`;
- esforço: até 2 h `+5`, até 5 h `+3`, até 8 h `0`, acima de 8 h `-3`.

Faixas: urgente a partir de 65, alta a partir de 45, média a partir de 25 e baixa abaixo disso. O planejador ordena os compromissos por prioridade e marca o ponto em que as horas ultrapassam o limite.

O banco guarda fatos. Pontuação, texto explicativo, porcentagem e encaixe são calculados a cada leitura para não ficarem desatualizados.

Datas de planejamento usam a zona `America/Fortaleza`, configurável em `Planning:TimeZone`. O relógio de produção converte o instante UTC para essa zona antes de obter o dia; assim um contêiner em UTC não adianta o prazo perto da meia-noite local.

## Testar e verificar

Backend:

```bash
dotnet test CabeNaSemana.slnx --configuration Release
dotnet format CabeNaSemana.slnx --verify-no-changes
dotnet list CabeNaSemana.slnx package --vulnerable --include-transitive
```

Frontend:

```bash
cd src/CabeNaSemana.Frontend
npm ci
npm run lint
npm run typecheck
npm test
npm run test:coverage
npm run test:e2e
npm run build
npm audit
```

A evidência TDD está registrada em [docs/testing/cabe-na-semana-mvp.tdd.md](docs/testing/cabe-na-semana-mvp.tdd.md).

## Segurança aplicada ao MVP

- `.env` e bancos locais são ignorados pelo Git e pelo Docker.
- PostgreSQL só é publicado em loopback.
- A API conecta com um papel runtime sem `SUPERUSER`, `CREATEDB`, `CREATEROLE`, replicação, bypass de RLS ou herança de outros papéis; o job de bootstrap concede apenas conexão, uso/criação do schema e CRUD da aplicação.
- API e frontend rodam como usuários sem root, sem capabilities e com `no-new-privileges`.
- O proxy do Next aceita apenas hosts configurados, reduzindo risco de DNS rebinding no ambiente local.
- O frontend envia CSP, `frame-ancestors`, `nosniff`, Referrer-Policy e Permissions-Policy; `X-Powered-By` está desativado.
- A API valida DTOs e o Domain protege invariantes.
- EF Core parametriza acesso ao banco.
- JSON usa enums textuais camelCase e recusa números.
- `ProblemDetails` padroniza `400`, `404`, `422`, `429` e `500`.
- Erros inesperados não expõem mensagens técnicas.
- O rate limit padrão é 120 requisições por minuto e responde com `Retry-After`.
- React escapa texto interpolado; não há renderização de HTML fornecido pelo usuário.

Não existem cookies, sessão ou autenticação neste MVP. Ao adicionar autenticação por cookie, a proteção contra CSRF deve ser projetada junto com ela.

## Limites conscientes

- uso individual e quadro global;
- schema criado com `EnsureCreated`, ainda sem migrations;
- para permitir esse primeiro boot, o papel runtime ainda possui `CREATE` no schema da aplicação; migrations executadas por um job administrativo permitiriam removê-lo;
- sem concorrência otimista entre vários usuários;
- sem calendário, recorrência, notificações ou colaboração;
- prioridade baseada em heurística fixa e transparente;
- Compose preparado para desenvolvimento/demonstração local, não para produção pública.

Próximos passos proporcionais: migrations, integração PostgreSQL em CI, autenticação/autorização, dados por usuário/semana, readiness do banco, logs estruturados e tratamento de concorrência.
