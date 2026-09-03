# Evidência TDD — Cabe na Semana MVP

## Escopo verificado

- regras de entidade, prioridade e capacidade;
- casos de uso de leitura, criação, edição, movimento, exclusão e configuração;
- persistência EF Core e seleção SQLite/PostgreSQL;
- contrato HTTP, DTOs, enums e ProblemDetails;
- formulários, cliente HTTP e estado React;
- drag-and-drop por mouse e teclado;
- alternativa acessível pelo menu **Mover**;
- build e execução real de Next + API + PostgreSQL no Docker.

## Ciclos RED → GREEN desta reestruturação

| Ciclo | RED observado | GREEN alcançado |
|---|---|---|
| API dedicada | Os testes não compilavam porque `CabeNaSemana.Api`, seus DTOs e suas rotas ainda não existiam. | Controllers, contratos e composition root implementados; CRUD e capacidade passaram no host em memória. |
| ID da criação | O serviço não devolvia o identificador necessário para `201 Created` e `Location`. | `OperationResult<T>` passou a transportar o ID sem acoplar Application ao HTTP. |
| Erros HTTP | Faltavam respostas verificáveis para 404, 422, 429, 500 e rota desconhecida. | ProblemDetails padronizado, rate limit e mensagem 500 sem detalhe técnico. |
| Frontend Next | Quatro suítes falharam porque componentes, estado e cliente HTTP ainda não existiam. | 49 testes Vitest passaram após a implementação mínima e os refinos. |
| Movimento otimista | O teste exigia que o cartão mudasse imediatamente e voltasse se a API falhasse. | `moveTaskLocally` cria novo snapshot; `BoardApp` mantém e restaura o anterior no erro. |
| Escrita confirmada, leitura falhou | Um `PATCH` bem-sucedido seguido de falha no `GET` era anunciado como falha da gravação e revertia a tela, abrindo espaço para repetição indevida. | A mutação e a sincronização posterior passaram a ter resultados distintos; o sucesso confirmado não é desfeito e a UI orienta uma nova leitura. |
| Mutações concorrentes | Cliques rápidos conseguiam iniciar duas gravações sobre o mesmo snapshot e disputar o estado React. | Um lock atômico serializa criação, edição, movimento, exclusão e capacidade; os testes cobrem o bloqueio e a liberação no `finally`. |
| Foco após mover | Depois do movimento, teclado e leitor de tela podiam perder o ponto de interação porque o cartão mudava de coluna. | O foco retorna para a alça do cartão movido e uma região viva anuncia origem, destino e resultado. |
| Drag em navegador | O Playwright inicialmente não completava o movimento por teclado. | Um coordinate getter específico do Kanban passou a saltar entre as colunas; mouse e teclado foram comprovados. |
| Medição do drag por teclado | O `KeyboardSensor` podia receber a seta antes de o dnd-kit disponibilizar `droppableRects`, mantendo o cartão na coluna de origem. | As colunas ganharam `data-kanban-status`; o getter usa a geometria do DOM como fallback e o E2E confirma `PATCH` real. |
| Lockfile no Docker | O `npm ci` da imagem, com npm 11.19, recusou dependências transitivas ausentes no lock gerado por npm 11.6. | O lock foi regenerado com a mesma versão da imagem e o build dos dois Dockerfiles passou. |
| Seed one-shot e concorrente | A API podia colidir com uma configuração existente e, depois que a pessoa apagava todas as tarefas, recriar dados de demonstração no próximo boot. Duas inicializações simultâneas também disputavam o seed. | Um marcador em `AppInitialization`, reivindicado com `ON CONFLICT DO NOTHING` dentro de transação e da execution strategy do EF, torna o seed único, preserva uso posterior e tolera concorrência. |
| Data local do planejamento | Perto da meia-noite UTC, `UtcNow.Date` podia representar o dia seguinte em Fortaleza. | `SystemAppClock` converte o instante via `TimeProvider` para `America/Fortaleza`; testes fixam a fronteira de data. |
| Healthcheck do frontend | O primeiro teste falhou porque a rota leve `/health` ainda não existia; o Compose verificava `/` e causava renderização e consultas periódicas ao banco. | A rota passou a responder `healthy` sem acessar a API; os logs ficaram silenciosos entre as verificações. |
| Host e headers do Next | Um `Host` arbitrário era aceito pelo proxy e a resposta não possuía a política de segurança esperada. | Allowlist explícita bloqueia DNS rebinding e o Next envia CSP, `frame-ancestors`, `nosniff`, Referrer-Policy e Permissions-Policy sem expor `X-Powered-By`. |
| Menor privilégio no PostgreSQL | A verificação retornou código `1`: o papel runtime `cabe_runtime` ainda não existia e a API usava o mesmo superusuário do bootstrap. | O job idempotente criou/reconciliou o papel sem privilégios administrativos, validou login e grants, e a API respondeu `200` conectada por esse papel. |
| Senha PostgreSQL com delimitadores | Uma senha contendo `;` e `=` podia ser interpretada como novos campos se a connection string fosse concatenada manualmente. | `PostgreSqlConnectionStringFactory` usa `NpgsqlConnectionStringBuilder` com valores separados; a pilha real iniciou e operou com esses caracteres. |
| Retry do PostgreSQL | A primeira imagem da API falhou porque uma transação manual foi aberta fora da execution strategy resiliente do Npgsql. | A inicialização inteira passou a executar dentro de `CreateExecutionStrategy().ExecuteAsync`, permitindo repetir a unidade transacional com segurança. |
| Fuso horário no Alpine | A imagem mínima não continha os dados de zona necessários para `America/Fortaleza`. | `tzdata` foi incluído na imagem final e o boot real comprovou a conversão de data. |

## Especificação executável

| Camada | Garantias principais | Evidência |
|---|---|---|
| Domain | Invariantes, timestamps, pesos de prioridade, atraso, faixas, consumo e overflow. | `StudyTaskTests`, `PriorityEngineTests`, `WeeklyCapacityPlannerTests` |
| Application | Orquestração, ausência, persistência, relógio e snapshot com quatro colunas. | `BoardServiceTests` |
| Infrastructure | Round-trip EF, provider configurável, connection string segura e inicialização one-shot concorrente. | `EfBoardRepositoryTests`, `PersistenceRegistrationTests`, `DatabaseInitializerTests` |
| API | CRUD, `201 + Location`, enum textual, recusa de número, validação, 404/422/429/500 e health. | `ApiContractTests`, `ApiErrorHandlingTests` |
| Frontend | Formulário, labels, transformação imutável, cliente ProblemDetails, sucesso/erro, rollback, lock, foco, host confiável e healthcheck. | 10 arquivos de teste Vitest |
| E2E | Arraste com mouse, sensor de teclado, menu alternativo e auditoria Axe. | `tests/e2e/kanban.spec.ts` |
| Docker | Build reprodutível, três serviços saudáveis, bootstrap encerrado com código `0`, leitura/escrita real e persistência. | Compose executado localmente |

## Resultado final reproduzido

```text
dotnet test CabeNaSemana.slnx --configuration Release
GREEN: 51/51 testes

Cobertura de todos os assemblies do backend
GREEN: 91,44% linhas; 83,22% branches
Domain: 95,21% linhas
Application: 92,70% linhas
Infrastructure: 98,70% linhas
API: 83,46% linhas

npm test
GREEN: 49/49 testes em 10 arquivos

npm run test:coverage
GREEN: 88,12% linhas; 87,05% funções; 78,33% branches

npm run test:e2e
GREEN: 4/4 testes Chromium

npm run lint
npm run typecheck
npm run build
GREEN: todos aprovados; rota / dinâmica

dotnet list CabeNaSemana.slnx package --vulnerable --include-transitive
npm audit
GREEN: nenhuma vulnerabilidade reportada
```

`dotnet format CabeNaSemana.slnx --verify-no-changes` e `git diff --check` também fazem parte do gate final.

## Inspeção real da pilha

- `docker compose build` publicou as imagens `cabe-na-semana-frontend:local` e `cabe-na-semana-api:local`.
- `docker compose ps -a` mostrou `web`, `api` e `postgres` saudáveis e `postgres-bootstrap` encerrado com código `0`.
- O PostgreSQL mostrou uma conexão da API por `cabe_runtime`; esse papel possui `NOSUPERUSER`, `NOCREATEDB` e `NOCREATEROLE`.
- Uma segunda pilha com nome, portas, credenciais e volume temporários confirmou o primeiro boot completo: bootstrap `0`, criação do schema pela API runtime e `GET /api/board` igual a `200`; o volume temporário foi removido depois do teste.
- A mesma prova passou com uma senha runtime contendo `;` e `=`; `NpgsqlConnectionStringBuilder` recebeu usuário, senha, servidor, porta e banco como valores separados e serializou a connection string corretamente.
- `GET /` retornou 200.
- `GET /api/board`, atravessando o rewrite do Next, retornou quatro colunas e cinco tarefas.
- O PostgreSQL manteve `WeeklyCapacityHours = 15.00` depois da troca de arquitetura.
- Uma tarefa temporária recebeu `201`, foi movida por `PATCH`, apareceu em `inProgress` e foi apagada com `204`.
- Um navegador real criou uma tarefa temporária, arrastou o cartão para **Em andamento**, confirmou a persistência e limpou o dado.
- Desktop 1440 px e mobile 390 px não apresentaram overflow horizontal nem erros de console.
- Axe não encontrou violações automáticas sérias ou críticas na jornada avaliada.

## Limites dos testes

- Axe automatiza parte da acessibilidade; navegação manual com leitor de tela ainda é recomendada antes de produção.
- O E2E do frontend usa uma API controlada para ser determinístico; a integração Docker foi exercitada separadamente com os serviços reais.
- Não há matriz Safari/Firefox nem CI com PostgreSQL efêmero nesta versão.
- O health endpoint comprova vida do processo, não readiness profunda do banco.
- Concorrência multiusuário e migrations ainda estão fora do MVP.
