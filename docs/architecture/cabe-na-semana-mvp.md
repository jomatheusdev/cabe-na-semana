# Cabe na Semana — guia de arquitetura do MVP

Este documento foi escrito para explicar o projeto em uma entrevista. A ideia não é decorar nomes de tecnologias, mas conseguir ligar cinco pontos: **problema, decisão, implementação, evidência e limite**.

## 1. Primeiro: o que “MVP” significa aqui

Neste projeto, **MVP significa Minimum Viable Product — Produto Mínimo Viável**. Não significa Model–View–Presenter, e também não é sinônimo de MVC.

O MVP é a menor versão capaz de testar a hipótese central do produto de ponta a ponta:

> Se uma pessoa enxergar, no mesmo quadro, a prioridade explicada de cada atividade e o consumo da sua capacidade semanal, ela conseguirá renegociar o plano antes de assumir mais trabalho do que cabe.

Para testar essa hipótese, a primeira entrega precisa permitir:

1. definir quantas horas existem na semana;
2. cadastrar uma atividade com prazo, importância e esforço;
3. organizar as atividades em quatro etapas do Kanban;
4. calcular prioridade de maneira transparente;
5. mostrar o que cabe e o que ultrapassa a capacidade;
6. editar, mover e excluir para revisar o plano;
7. persistir os dados para que a demonstração seja real.

Autenticação, colaboração, notificações, recorrência, calendário, subtarefas e microserviços ficaram fora. Eles podem ser úteis, mas não são necessários para responder à hipótese acima.

Uma boa frase para a entrevista é:

> “Eu não tentei construir uma plataforma inteira de produtividade. Recortei uma decisão específica — descobrir se os compromissos cabem na semana — e entreguei essa jornada completa, testável e demonstrável.”

## 2. Por que esta arquitetura existe

O produto é pequeno, mas a regra não deve ficar presa à tela nem ao banco. A arquitetura separa partes que mudam por motivos diferentes:

- **Domain** muda quando muda a regra do produto.
- **Application** muda quando muda a sequência de um caso de uso.
- **Infrastructure** muda quando muda a tecnologia de persistência.
- **API** muda quando muda o contrato HTTP.
- **Frontend** muda quando muda a experiência da pessoa usuária.

Essa separação evita dois problemas comuns:

- colocar cálculo de prioridade dentro de um componente React ou controller;
- espalhar EF Core, SQL e detalhes do PostgreSQL por toda a aplicação.

O resultado não é um conjunto de microserviços. É uma aplicação pequena, organizada em um único repositório, com dois processos de borda no Docker: o frontend Next.js e a API ASP.NET Core. O backend continua sendo um **monólito modular em camadas**: um único serviço de API, dividido em projetos com fronteiras de compilação.

## 3. Por que o frontend agora é Next.js

A primeira interface poderia ser server-rendered pelo próprio ASP.NET Core. Isso teria menos peças e seria uma decisão válida para um formulário simples. Depois que o requisito passou a incluir uma interface dedicada em Next.js e movimentação por arrastar, um frontend React ficou proporcional por três razões:

1. o Kanban tem estado interativo no navegador;
2. o movimento otimista deixa a ação imediata e permite reverter o cartão se a API falhar;
3. uma API explícita separa a experiência visual da regra e pode atender outros clientes no futuro.

Next.js foi escolhido em vez de React “solto” porque já oferece estrutura de rotas, renderização no servidor, tratamento de carregamento/erro, build de produção e proxy por `rewrites`. A página inicial é um Server Component: ela busca o primeiro quadro no servidor. A partir daí, `BoardApp` e os componentes do Kanban assumem as interações no cliente.

Isso tem um custo real: agora existem duas toolchains, dois builds e dois processos. Para o MVP, o custo foi controlado mantendo tudo no mesmo repositório e evitando bibliotecas que ainda não são necessárias, como Redux, uma camada BFF própria ou um design system externo.

Resposta curta para a entrevista:

> “Next não entrou porque o .NET fosse incapaz de renderizar a tela. Ele entrou porque o produto passou a exigir uma borda interativa, com drag-and-drop e atualização otimista. Mantive a regra no .NET e usei React somente onde o estado de interface agrega valor.”

## 4. Por que o frontend fica dentro de `src/`

`src/` significa **código do produto**, não “somente código .NET”. Por isso o frontend cabe naturalmente ao lado dos projetos do backend:

```text
src/
├── CabeNaSemana.Domain/
├── CabeNaSemana.Application/
├── CabeNaSemana.Infrastructure/
├── CabeNaSemana.Api/
└── CabeNaSemana.Frontend/
```

Essa organização é um monorepo simples. Ela traz vantagens importantes para o estágio atual:

- uma alteração de contrato pode atualizar API, frontend e testes no mesmo commit;
- o Compose monta uma versão compatível dos três serviços de longa duração e do job de bootstrap;
- o README e o guia descrevem uma única fonte de verdade;
- a pessoa avaliadora clona um repositório e executa a solução inteira;
- o histórico mostra a evolução completa da funcionalidade.

Estar no mesmo `src` **não cria acoplamento de compilação**. O frontend não referencia assemblies .NET; ele depende apenas do contrato HTTP. Se equipes, ciclos de release ou permissões se tornarem independentes, o frontend poderá ser extraído para outro repositório sem mover Domain, Application ou Infrastructure.

## 5. Arquitetura lógica e direção das dependências

```text
Navegador
   │ HTML e chamadas /api
   ▼
Next.js Frontend ─ ─ HTTP/JSON ─ ─▶ ASP.NET Core API
                                         │
                                         ▼
                                   Application
                                      │     ▲
                                      ▼     │ contrato
                                    Domain  │
                                            │
                                   Infrastructure
                                         │ EF Core
                                         ▼
                             PostgreSQL ou SQLite
```

As setas importantes no backend são:

- `Application → Domain`;
- `Infrastructure → Application + Domain`;
- `Api → Application + Infrastructure`;
- `Domain → nenhuma camada externa`.

`IBoardRepository` fica em Application. A Application declara a capacidade de persistência de que precisa; Infrastructure implementa essa porta com EF Core. A API é o **composition root**: em `Program.cs`, ela liga `IBoardRepository` a `EfBoardRepository` e monta o pipeline HTTP.

O frontend não recebe entidade EF. Controllers convertem contratos HTTP em comandos de Application e convertem snapshots da Application em DTOs JSON. Isso impede que uma mudança de mapeamento do banco vire, por acidente, uma quebra do frontend.

## 6. Arquitetura de execução no Docker

```text
localhost:8080
      │
      ▼
web — Next.js :3000
      │  rewrite /api/*
      ▼
api — ASP.NET Core :8080
      │  EF Core/Npgsql como cabe_runtime
      ▼
postgres — PostgreSQL :5432
      │
      ▼
volume postgres_data
```

Somente o frontend é a entrada pública da aplicação. A API é encontrada pelo nome interno `api` na rede do Compose. O PostgreSQL também é acessível pelos contêineres como `postgres`; sua porta é publicada apenas em `127.0.0.1` para permitir inspeção local com `psql`, DBeaver ou DataGrip. Antes da API iniciar, o job descartável `postgres-bootstrap` cria ou reconcilia `cabe_runtime`, concede somente as permissões da aplicação e termina. Assim, o usuário administrativo não fica na conexão da API. `APP_DB_USER` e `APP_DB_PASSWORD` são obrigatórios e devem ser diferentes das credenciais administrativas; não existe fallback para a senha do superusuário.

O navegador chama `/api/...` no mesmo host do frontend. O `rewrite` do Next encaminha a chamada à API. Isso evita configurar CORS e mantém uma origem única no MVP.

## 7. Fluxo de leitura da página

1. O navegador solicita `/` ao serviço `web`.
2. `app/page.tsx`, executado no servidor Next, chama `GET /api/board` com `cache: "no-store"`.
3. `BoardController` chama `BoardService.GetAsync`.
4. `EfBoardRepository` lê tarefas e capacidade.
5. `PriorityEngine` recalcula a prioridade.
6. `WeeklyCapacityPlanner` ordena compromissos e calcula encaixe/sobrecarga.
7. A API devolve `BoardResponse` em JSON com enums textuais em camelCase.
8. Next renderiza o HTML inicial e hidrata `BoardApp` para as próximas interações.

Prioridade, porcentagem e encaixe não são salvos como fatos permanentes. Eles são derivados dos dados atuais a cada leitura. Isso evita valores calculados desatualizados depois de editar prazo, esforço, status ou capacidade.

## 8. Fluxo de criação e edição

1. `TaskForm` valida os requisitos básicos para oferecer feedback imediato.
2. O cliente envia JSON para `POST /api/tasks` ou `PUT /api/tasks/{id}`.
3. `[ApiController]` e Data Annotations validam o contrato de entrada.
4. `TasksController` transforma o DTO em `SaveTaskCommand`.
5. `BoardService` orquestra o caso de uso.
6. `StudyTask.Create` ou `StudyTask.Update` protege as invariantes do domínio.
7. `EfBoardRepository` registra a alteração e `SaveChangesAsync` confirma a unidade de trabalho.
8. O frontend solicita novamente o quadro para receber todos os cálculos derivados atualizados.

Existem validações em fronteiras diferentes por motivos diferentes. O formulário melhora a experiência; a API protege o limite HTTP; o Domain impede um estado inválido mesmo que, no futuro, outro cliente ou job contorne a interface atual.

## 9. Como funciona o arrastar no Kanban

O drag-and-drop é uma melhoria da interação, não uma nova regra de negócio.

1. `DndContext` registra sensores de mouse, toque e teclado.
2. `TaskCard` fornece uma alça explícita para iniciar o arraste.
3. Cada `KanbanColumnView` é uma área de destino.
4. Ao soltar, o frontend identifica o cartão e a coluna.
5. `moveTaskLocally` cria um novo snapshot sem alterar o objeto anterior.
6. A tela move o cartão imediatamente — atualização otimista.
7. O cliente envia `PATCH /api/tasks/{id}/status`.
8. Em sucesso, o quadro é recarregado para recalcular prioridade e capacidade.
9. Em falha, o snapshot anterior volta e uma mensagem segura é anunciada.

O arraste não é a única forma de concluir a tarefa. Cada cartão mantém o menu **Mover**, operável por teclado, mouse ou toque. A biblioteca anuncia início, destino, conclusão e cancelamento em uma região viva para leitores de tela. Essa redundância é intencional: drag-and-drop sozinho não é uma interação universal.

Resposta curta:

> “O React cuida do gesto e do estado otimista; a regra continua no backend. Soltar um cartão gera o mesmo PATCH que o botão acessível de movimento. Se a API falhar, eu reverto o snapshot em vez de fingir que persistiu.”

## 10. Por que a URL não usa `/v1` ou `/v2`

O MVP tem um único frontend e um único contrato ainda não publicado para consumidores externos. Colocar `/v1` agora criaria a impressão de que duas gerações precisam coexistir, sem existir esse requisito.

As rotas usam o recurso diretamente:

- `/api/board`;
- `/api/tasks`;
- `/api/settings/weekly-capacity`.

Mudanças compatíveis podem evoluir o contrato atual. Versionamento será introduzido quando houver uma quebra incompatível e clientes que não possam migrar juntos. Nesse cenário, a equipe pode escolher versão por URL, cabeçalho ou media type com base nos consumidores reais.

## 11. Contrato HTTP

| Método e rota | Responsabilidade | Sucesso principal |
|---|---|---|
| `GET /api/board` | Carregar quadro, prioridade e capacidade | `200` + `BoardResponse` |
| `GET /api/tasks/{id}` | Buscar os fatos editáveis de uma atividade | `200` + `TaskResponse` |
| `POST /api/tasks` | Criar uma atividade | `201` + ID e `Location` |
| `PUT /api/tasks/{id}` | Substituir os campos editáveis | `204` |
| `PATCH /api/tasks/{id}/status` | Mover uma atividade no Kanban | `204` |
| `DELETE /api/tasks/{id}` | Excluir uma atividade | `204` |
| `PUT /api/settings/weekly-capacity` | Ajustar a capacidade semanal | `204` |
| `GET /health` | Informar vida do processo | `200` + `healthy` |

Enums são enviados como strings camelCase, por exemplo `critical`, `thisWeek` e `inProgress`. Números são recusados para impedir que a ordem interna de um enum vire contrato público por acidente.

Erros seguem `ProblemDetails`:

- `400` para JSON/model binding inválido;
- `404` para atividade inexistente;
- `422` para uma regra de domínio rejeitada;
- `429` para limite de requisições;
- `500` com mensagem genérica e `traceId`, sem detalhe técnico.

## 12. Dados persistidos e dados calculados

`StudyTasks` guarda fatos informados ou produzidos por uma ação:

- `Id`;
- `Title`;
- `DueDate`;
- `Importance`;
- `EstimatedHours`;
- `Status`;
- `CreatedAtUtc`;
- `UpdatedAtUtc`.

`PlannerSettings` tem um registro `Id = 1` com `WeeklyCapacityHours`. Esse singleton reflete o limite atual do MVP: existe um quadro individual e uma capacidade global. Uma versão multiusuário precisaria associar configurações e tarefas a um usuário e, provavelmente, a uma semana.

Pontuação, nível de prioridade, texto explicativo, percentual utilizado e encaixe na capacidade são calculados. Persistir esses valores duplicaria a regra e exigiria sincronização sempre que um fato mudasse.

## 13. Mapa das pastas e arquivos

Antes dos arquivos, a divisão de primeiro nível responde “que tipo de coisa vive aqui?”:

| Pasta | Conteúdo | Motivo da fronteira |
|---|---|---|
| `src/` | Todo o código-fonte do produto: frontend e backend. | Agrupa o que é publicado como plataforma, sem confundir linguagem com responsabilidade. |
| `tests/` | Suíte xUnit do backend, espelhada por camada. | Mantém a especificação executável fora dos assemblies de produção. |
| `docs/` | Arquitetura e evidência de testes. | Decisões e provas continuam acessíveis depois da entrevista. |
| `docker/` | Scripts operacionais específicos dos contêineres. | O bootstrap do banco pertence à operação, não ao domínio do produto. |
| `.vscode/` | Preferências compartilháveis do editor. | Ajuda o onboarding sem alterar a lógica ou o build. |

### Raiz do repositório

| Caminho | Para que serve | Por que existe aqui |
|---|---|---|
| `.dockerignore` | Exclui Git, segredos, builds .NET/Next e relatórios do contexto Docker da API. | Reduz tempo, tamanho e risco de copiar algo indevido. |
| `.editorconfig` | Uniformiza charset, indentação e convenções de C#. | Editor e CI aplicam a mesma base. |
| `.env.example` | Documenta banco, papéis administrativo/runtime, senhas distintas e portas. | Ensina a configuração sem versionar o `.env` real. |
| `.gitignore` | Ignora segredos, bancos locais, dependências e artefatos de teste/build. | Mantém o histórico focado no código-fonte. |
| `.vscode/settings.json` | Oculta arquivos gerados do explorador sem apagá-los. | Deixa a árvore da entrevista legível e não interfere no build. |
| `CabeNaSemana.slnx` | Reúne os quatro projetos .NET e o projeto xUnit. | Um comando restaura, compila e testa o backend. |
| `compose.yaml` | Orquestra `web`, `api`, `postgres` e o job `postgres-bootstrap`, além de dependências, health checks, portas e volume. | Reproduz a pilha completa e separa a credencial administrativa do papel runtime. |
| `docker/postgres/bootstrap-runtime-role.sh` | Cria/reconcilia o papel PostgreSQL usado pela API e concede somente as permissões necessárias. | Aplica menor privilégio em volume novo ou existente. |
| `docker/postgres/verify-runtime-role.sh` | Falha se o papel runtime ganhar atributos administrativos, associação a papéis ou grants incorretos. | Transforma a política de banco em uma verificação executável. |
| `Directory.Build.props` | Aplica analisadores, nullable e qualidade comuns aos projetos .NET. | Evita repetir configuração em cada `.csproj`. |
| `Dockerfile.api` | Publica a API em build multi-stage, instala dados IANA de fuso e roda sem root. | Separa compilação do runtime e mantém America/Fortaleza disponível no Alpine. |
| `global.json` | Fixa a linha do SDK .NET usada pelo projeto. | Reduz diferenças entre máquinas e imagem Docker. |
| `README.md` | Explica instalação, execução, testes, banco, arquitetura e limites. | É a porta de entrada do repositório público. |

O arquivo `.env` existe somente na máquina local. Ele não aparece no Git nem deve ser aberto durante uma entrevista, porque contém a senha do PostgreSQL.

Em um volume PostgreSQL já criado, `POSTGRES_USER` identifica o papel administrativo original. Alterar apenas essa variável não recria o papel dentro do volume; por isso o nome deve ser preservado, ou a migração/recriação precisa ser feita conscientemente.

### `src/CabeNaSemana.Domain/`

| Arquivo | Responsabilidade |
|---|---|
| `CabeNaSemana.Domain.csproj` | Biblioteca sem referência a API, frontend ou EF Core. |
| `Tasks/StudyTask.cs` | Entidade; cria, edita e move preservando invariantes e timestamps. |
| `Tasks/Importance.cs` | Vocabulário tipado de importância. |
| `Tasks/KanbanColumn.cs` | Quatro etapas e regra de quais colunas consomem capacidade. |
| `Tasks/DomainValidationException.cs` | Transporta erros estruturados sem depender de HTTP/ModelState. |
| `Planning/PriorityEngine.cs` | Calcula pontuação, faixa e explicação de prioridade. |
| `Planning/PriorityAssessment.cs` | Resultado imutável da avaliação de prioridade. |
| `Planning/WeeklyCapacityPlanner.cs` | Ordena compromissos e calcula encaixe, uso e sobrecarga. |
| `Planning/WeeklyPlan.cs` | Tipos de saída do planejamento semanal. |

Essa é a camada mais interna. Se Next, PostgreSQL e até ASP.NET Core forem trocados, essas regras continuam válidas.

### `src/CabeNaSemana.Application/`

| Arquivo | Responsabilidade |
|---|---|
| `CabeNaSemana.Application.csproj` | Biblioteca que referencia apenas Domain. |
| `Board/BoardContracts.cs` | Comando de gravação e snapshots devolvidos pelos casos de uso. |
| `Board/BoardService.cs` | Orquestra leitura, criação, edição, movimento, exclusão e capacidade. |
| `Board/IBoardRepository.cs` | Porta de persistência exigida pelos casos de uso. |
| `Common/IAppClock.cs` | Abstrai data/hora para testes determinísticos. |
| `Common/SystemAppClock.cs` | Mantém timestamp em UTC e calcula a data de negócio no fuso configurado. |
| `Common/OperationResult.cs` | Representa sucesso, valor criado, validação ou ausência sem conhecer HTTP. |

Application não sabe se a chamada começou em React nem se os dados terminam no PostgreSQL. Ela conhece intenções do produto e interfaces.

### `src/CabeNaSemana.Infrastructure/`

| Arquivo | Responsabilidade |
|---|---|
| `CabeNaSemana.Infrastructure.csproj` | Declara EF Core, SQLite, Npgsql e referências internas. |
| `Persistence/PlannerDbContext.cs` | Mapeia entidade, colunas, conversões de enum, precisão e índice. |
| `Persistence/EfBoardRepository.cs` | Implementa `IBoardRepository` com consultas e unidade de trabalho EF. |
| `Persistence/PersistenceRegistration.cs` | Seleciona `Sqlite` ou `PostgreSql` pela configuração. |
| `Persistence/PostgreSqlConnectionStringFactory.cs` | Monta a conexão com `NpgsqlConnectionStringBuilder` a partir de campos separados. |
| `Persistence/DatabaseInitializer.cs` | Cria o schema e reivindica o seed uma única vez em transação compatível com retry. |
| `Persistence/PlannerSetting.cs` | Modelo persistente da capacidade semanal global. |

Infrastructure depende da porta da Application, e não o contrário. Essa é a inversão de dependência aplicada de forma prática.

### `src/CabeNaSemana.Api/`

| Arquivo | Responsabilidade |
|---|---|
| `CabeNaSemana.Api.csproj` | Executável web .NET e referências a Application/Infrastructure. |
| `Program.cs` | Composition root, JSON, ProblemDetails, rate limit, DI, banco, rotas e health. |
| `appsettings.json` | Provider local, fuso de negócio, logs e limite padrão. |
| `appsettings.Development.json` | Verbosidade adequada ao desenvolvimento. |
| `Properties/launchSettings.json` | Perfil local previsível em `http://localhost:5147`. |
| `Contracts/TaskRequests.cs` | DTOs e validações de criar/editar/mover. |
| `Contracts/TaskResponses.cs` | DTO de leitura e resposta `201` com o ID criado. |
| `Contracts/BoardResponses.cs` | Projeções JSON do quadro, colunas, cartões e capacidade. |
| `Contracts/SettingsRequests.cs` | Entrada para capacidade semanal. |
| `Controllers/ApiControllerBase.cs` | Traduz resultados esperados em ProblemDetails 404/422. |
| `Controllers/BoardController.cs` | Expõe somente a leitura agregada do quadro. |
| `Controllers/TasksController.cs` | Expõe CRUD e movimento, convertendo DTO em comando. |
| `Controllers/SettingsController.cs` | Expõe a alteração da capacidade. |

Controllers são adaptadores. Eles conhecem HTTP, mas não implementam a fórmula de prioridade nem consultas EF.

### `src/CabeNaSemana.Frontend/`

| Arquivo ou pasta | Responsabilidade |
|---|---|
| `app/layout.tsx` | HTML raiz, metadados, cabeçalho, rodapé e link de salto. |
| `app/page.tsx` | Server Component dinâmico que busca o snapshot inicial sem cache. |
| `app/loading.tsx` | Estado exibido durante carregamento de rota. |
| `app/error.tsx` | Error Boundary do cliente com opção de tentar novamente. |
| `app/health/route.ts` | Resposta mínima usada pelo healthcheck do contêiner, sem consultar API ou banco. |
| `app/__tests__/health-route.test.ts` | Garante o contrato `200`, `text/plain` e corpo `healthy` da rota de saúde. |
| `app/globals.css` | Tokens visuais, layout, estados, Kanban, responsividade, foco e movimento reduzido. |
| `features/board/BoardApp.tsx` | Estado do quadro e coordenação de todas as mutações/refetch/feedback. |
| `features/board/CapacityPanel.tsx` | Progresso, sobrecarga e formulário de capacidade. |
| `features/board/TaskForm.tsx` | Formulário controlado, validação cliente e mapeamento do comando. |
| `features/board/EditTaskDialog.tsx` | Diálogo acessível de edição do cartão. |
| `features/board/KanbanBoard.tsx` | Contexto, sensores, overlay e anúncios do drag-and-drop. |
| `features/board/KanbanColumn.tsx` | Área de destino e estado vazio de cada coluna. |
| `features/board/TaskCard.tsx` | Conteúdo do cartão, alça de arraste e ações alternativas. |
| `features/board/board-state.ts` | Transformações imutáveis para localizar e mover cartões localmente. |
| `features/board/keyboard-coordinates.ts` | Traduz as setas do teclado em saltos previsíveis entre colunas do Kanban. |
| `features/board/labels.ts` | Textos de enums, prazo, prioridade e capacidade em português. |
| `features/board/types.ts` | Contrato TypeScript espelhado da API. |
| `lib/api-client.ts` | `fetch`, serialização e tradução de ProblemDetails em erro de UI. |
| `lib/trusted-hosts.ts` | Normaliza o header Host e compara loopback/allowlist explícita. |
| `proxy.ts` | Interrompe requisições com Host desconhecido antes da página ou do rewrite da API. |
| `public/.gitkeep` | Mantém o diretório público disponível mesmo antes de haver assets. |
| `tests/fixtures/board.ts` | Snapshot previsível reutilizado pelos testes de componente. |
| `tests/e2e/kanban.spec.ts` | Jornada real de arraste e alternativa por teclado no Chromium. |
| `tests/e2e/mock-api.mjs` | API em memória exclusiva do teste E2E do frontend. |
| `features/board/__tests__/...` | Testes de formulário, capacidade, Kanban, labels, estado, rollback e coordenadas de teclado. |
| `lib/__tests__/api-client.test.ts` | Testes da interpretação de ProblemDetails. |
| `lib/__tests__/trusted-hosts.test.ts` | Testa loopback, allowlist e hosts malformados/hostis. |
| `__tests__/proxy.test.ts` | Confirma bloqueio 400 e passagem de requests locais. |
| `Dockerfile` | Build Next multi-stage, saída standalone e usuário não-root. |
| `.dockerignore` | Remove dependências, cache e relatórios do contexto do frontend. |
| `.gitignore` | Impede o versionamento de `.next`, cobertura, relatórios E2E e caches TypeScript. |
| `AGENTS.md` | Aviso gerado pelo Next para ferramentas de desenvolvimento consultarem a documentação instalada. |
| `CLAUDE.md` | Encaminha outras ferramentas de assistência para as mesmas instruções de `AGENTS.md`. |
| `next.config.ts` | Ativa standalone, remove `X-Powered-By`, envia headers de segurança e encaminha `/api/*`. |
| `package.json` | Scripts e dependências diretas do frontend. |
| `package-lock.json` | Árvore exata e reproduzível de pacotes npm. |
| `playwright.config.ts` | Servidores de teste, navegador e coleta de evidências E2E. |
| `vitest.config.ts` | Ambiente jsdom, alias e cobertura unitária. |
| `vitest.setup.ts` | Matchers de DOM e limpeza entre testes. |
| `eslint.config.mjs` | Regras estáticas do Next e TypeScript. |
| `tsconfig.json` | Tipagem estrita, alias `@/` e configuração do compilador. |
| `next-env.d.ts` | Tipos gerados/esperados pelo Next. |

O frontend é organizado por feature porque arquivos que mudam juntos devem ficar próximos. `app/` trata a borda do framework; `features/board/` contém a experiência do produto; `lib/` isola a comunicação HTTP.

### `tests/CabeNaSemana.Tests/`

| Caminho | Responsabilidade |
|---|---|
| `CabeNaSemana.Tests.csproj` | xUnit, host de integração, SQLite em memória e cobertura. |
| `Domain/StudyTaskTests.cs` | Invariantes, atualização e movimento da entidade. |
| `Domain/PriorityEngineTests.cs` | Pesos, faixas, atraso e explicações. |
| `Domain/WeeklyCapacityPlannerTests.cs` | Consumo, ordem, limite e overflow. |
| `Application/BoardServiceTests.cs` | Casos de uso com repositório/relógio controlados. |
| `Application/SystemAppClockTests.cs` | Garante que 01:30 UTC ainda pertence ao dia anterior em Fortaleza. |
| `Infrastructure/EfBoardRepositoryTests.cs` | Round-trip real via EF e SQLite em memória. |
| `Infrastructure/PersistenceRegistrationTests.cs` | Providers e montagem segura de senha PostgreSQL com caracteres especiais. |
| `Infrastructure/DatabaseInitializerTests.cs` | Seed parcial, one-shot após exclusão e inicialização concorrente. |
| `Api/ApiContractTests.cs` | Rotas, JSON, CRUD, enum, validação, status e health. |
| `Api/ApiErrorHandlingTests.cs` | 404 genérico, 422, 429 e 500 sem vazamento técnico. |

Os testes .NET ficam em um projeto único porque o backend ainda é pequeno; as pastas preservam a intenção por camada sem multiplicar configuração.

### `docs/`

| Caminho | Responsabilidade |
|---|---|
| `architecture/cabe-na-semana-mvp.md` | Registra arquitetura, decisões e roteiro de entrevista. |
| `testing/cabe-na-semana-mvp.tdd.md` | Registra RED/GREEN, comandos, cobertura e jornadas verificadas. |

### Diretórios gerados ou locais que não representam arquitetura

| Caminho | Quem cria | Por que não é explicado como código do produto |
|---|---|---|
| `.git/` | Git | Histórico, refs e metadados de versionamento. |
| `.DS_Store` | macOS | Preferência visual do Finder; é ignorada. |
| `**/bin/` e `**/obj/` | SDK .NET | Binários e arquivos intermediários reproduzíveis pelo build. |
| `node_modules/` | `npm ci` | Dependências baixadas a partir de `package-lock.json`. |
| `.next/` e `tsconfig.tsbuildinfo` | Next.js/TypeScript | Saída e cache de compilação, não fonte. |
| `coverage/`, `TestResults/`, `playwright-report/` e `test-results/` | Ferramentas de teste | Evidências locais regeneráveis; os resultados resumidos ficam em `docs/testing/`. |
| `src/CabeNaSemana.Api/Data/` | Execução local da API | Guarda o SQLite de desenvolvimento; no Docker os dados ficam no volume PostgreSQL. |
| `.env` | Pessoa desenvolvedora | Contém configuração e senhas locais; nunca deve entrar no Git ou numa apresentação. |

Essa distinção é útil na entrevista: arquivo gerado pode aparecer no VS Code, mas isso não significa que deva ser versionado, documentado individualmente ou tratado como uma decisão de domínio.

## 14. Como explicar Docker sem decorar comandos

- **Imagem** é o pacote imutável com runtime e aplicação.
- **Contêiner** é uma execução dessa imagem.
- **Dockerfile** ensina a construir uma imagem.
- **Compose** descreve como vários contêineres trabalham juntos.
- **Volume** guarda dados fora do ciclo de vida do contêiner.
- **Health check** informa se um serviço está pronto para ser usado.

O build é multi-stage. As imagens de SDK/Node com ferramentas completas compilam o código; as imagens finais recebem somente o necessário para executar. API e frontend rodam como usuários sem privilégios. O Compose ainda remove capabilities e ativa `no-new-privileges`.

`depends_on` organiza a partida: PostgreSQL fica saudável, `postgres-bootstrap` reconcilia o papel runtime e termina com sucesso, a API fica saudável e só então o frontend é liberado. Isso reduz erros de inicialização, mas a conexão Npgsql também usa retry porque dependências reais podem oscilar depois do startup.

## 15. Segurança aplicada ao escopo

- `.env` não é versionado nem enviado ao contexto Docker.
- PostgreSQL é publicado apenas em loopback.
- A API usa um papel runtime sem privilégios administrativos e sem associação a outros papéis; o superusuário de bootstrap não aparece em sua connection string.
- API e frontend rodam sem root e sem capabilities Linux.
- Entradas são validadas na API e no Domain.
- EF Core parametriza os comandos de banco.
- Enums numéricos são recusados.
- Erros inesperados não expõem stack trace nem mensagens internas.
- O rate limit reduz abuso acidental e devolve `Retry-After`.
- O proxy do Next aceita somente loopback ou hosts configurados, reduzindo DNS rebinding no uso local.
- CSP, `frame-ancestors`, `nosniff`, Referrer-Policy e Permissions-Policy endurecem as respostas.
- React escapa texto interpolado por padrão; não há renderização de HTML fornecido pelo usuário.
- Não existem cookies, sessão ou autenticação neste MVP; por isso uma requisição cross-site não carrega uma credencial de usuário a ser explorada. Quando autenticação por cookie entrar, a estratégia de CSRF deverá entrar junto.

O Compose é endurecido para desenvolvimento/demonstração local. Produção ainda exigiria TLS, secret manager, proxy confiável, observabilidade, backup, migrations, autorização e política de rede.

Como `EnsureCreated` permite que a API prepare um banco vazio, o papel runtime ainda recebe `CREATE` no schema da aplicação. Ele não administra o cluster, mas esse privilégio de DDL é um débito consciente do MVP. Com migrations aplicadas por um job administrativo, a API poderia ficar somente com `USAGE`, leitura, escrita e sequences.

## 16. Estratégia de testes

A pirâmide protege riscos diferentes:

1. **Domain:** regra rápida e isolada.
2. **Application:** sequência de caso de uso com fakes.
3. **Infrastructure:** mapeamento EF com SQLite real em memória.
4. **API:** contrato HTTP no host em memória.
5. **Frontend unitário/componente:** validação, estado imutável, erro e acessibilidade.
6. **E2E:** navegador real arrastando um cartão e usando a alternativa por teclado.
7. **Docker:** Next, API e PostgreSQL reais conectados.

O drag-and-drop não é considerado comprovado apenas porque uma função de movimento passou. O teste E2E move o ponteiro, espera o `PATCH`, verifica o corpo enviado e confirma o cartão na coluna de destino.

## 17. Decisões conscientes e limites

| Decisão atual | Benefício | Limite / evolução |
|---|---|---|
| Monorepo | Contrato e consumidores evoluem juntos. | Separar se ownership/releases divergirem. |
| API sem versão | Menos ruído para um único consumidor. | Versionar quando duas versões incompatíveis precisarem coexistir. |
| `EnsureCreated` | Onboarding e demo simples. | Adotar migrations em job administrativo e retirar `CREATE` do papel runtime. |
| SQLite local + PostgreSQL no Docker | Execução direta simples e banco servidor reproduzível. | Automatizar integração PostgreSQL em CI. |
| Estado local React | Poucas dependências e fluxo legível. | Avaliar biblioteca de cache somente quando complexidade justificar. |
| Atualização otimista no movimento | Feedback imediato. | Concorrência multiusuário exigirá versão/ETag e reconciliação. |
| Quadro global | Prova a hipótese com pouco cadastro. | Introduzir usuário, autorização e ownership de dados. |
| Heurística fixa | Decisão transparente e testável. | Medir uso e calibrar pesos sem transformar a explicação em caixa-preta. |

## 18. Roteiro natural para a entrevista

### Em aproximadamente 45 segundos

> “O Cabe na Semana é um MVP de planejamento que responde uma pergunta que uma lista comum não responde: as atividades realmente cabem nas horas disponíveis? A pessoa informa prazo, importância e esforço; o sistema calcula uma prioridade explicável, organiza o Kanban e mostra sobrecarga. O frontend é Next.js porque o quadro exige interação rica e drag-and-drop. A API é ASP.NET Core, e a regra continua isolada em Domain e Application. Com Docker, web, API e PostgreSQL sobem juntos.”

### Em aproximadamente 90 segundos

> “Eu comecei pela hipótese do produto, não pela tecnologia: visualizar prioridade e capacidade deve ajudar a renegociar uma semana inviável. Delimitei um fluxo individual, quatro colunas e uma capacidade global. No backend, Domain guarda a entidade e os cálculos; Application orquestra casos de uso e declara o repositório; Infrastructure implementa persistência com EF Core; API traduz HTTP e monta as dependências. O Next renderiza o primeiro snapshot no servidor e assume as interações no cliente. No Kanban, o movimento é otimista: o cartão muda imediatamente, a API recebe um PATCH e, se algo falhar, o estado anterior volta. O gesto também possui botões e anúncios acessíveis. Mantive frontend e backend no mesmo src porque são partes do mesmo produto e do mesmo release, sem criar referência de compilação entre eles. A pilha Docker mantém três serviços de longa duração, executa um job curto de bootstrap com menor privilégio e persiste o PostgreSQL em volume. Testei regra, casos de uso, EF, contrato HTTP, componentes e a jornada real de arraste.”

## 19. Perguntas comuns

**É MVC?**

Não como arquitetura da interface atual. O frontend usa componentes React/Next e o backend expõe controllers de API. “MVP” aqui descreve Produto Mínimo Viável, não um padrão de apresentação.

**É Clean Architecture?**

É uma arquitetura em camadas inspirada na regra de dependência da Clean Architecture. A descrição evita afirmar a adoção completa de todas as cerimônias do padrão.

**Por que não colocar a regra no Next?**

Porque outros clientes poderiam precisar da mesma decisão e porque o servidor deve proteger a consistência, independentemente do navegador.

**Por que não microserviços?**

O domínio e a equipe não exigem deploys independentes. Microserviços adicionariam rede, observabilidade, consistência distribuída e operação sem provar melhor a hipótese.

**Por que PostgreSQL se existe SQLite?**

SQLite reduz atrito na execução direta. PostgreSQL no Docker demonstra um banco servidor real. A Application não muda porque o provider está atrás do mesmo repositório.

**Por que refazer o GET depois de uma mutação?**

Porque prioridade e capacidade são derivadas. O refetch usa o backend como fonte de verdade e evita duplicar a fórmula em TypeScript.

**Por que manter o botão Mover com drag-and-drop?**

Porque o gesto não é adequado para todas as pessoas e dispositivos. Os dois caminhos disparam o mesmo caso de uso.

## 20. Comandos para a demonstração

```bash
# pilha completa
docker compose up --build -d
docker compose ps
docker compose logs --tail=30 web api postgres postgres-bootstrap

# testes .NET
dotnet test CabeNaSemana.slnx --configuration Release

# qualidade e testes Next
cd src/CabeNaSemana.Frontend
npm ci
npm run lint
npm run typecheck
npm test
npm run test:e2e
npm run build

# abrir o banco
docker compose exec postgres sh -lc 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"'
```

Dentro do `psql`:

```sql
\dt
SELECT "Title", "DueDate", "Importance", "EstimatedHours", "Status"
FROM "StudyTasks";
SELECT * FROM "PlannerSettings";
\q
```

Antes de compartilhar a tela, confirme que `docker compose ps -a` mostra `web`, `api` e `postgres` saudáveis e `postgres-bootstrap` como `Exited (0)`, feche o `.env` e deixe abertas somente as abas que ajudam a explicar o fluxo.
