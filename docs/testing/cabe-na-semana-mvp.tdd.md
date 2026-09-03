# Evidência TDD — Cabe na Semana MVP

## Origem

As jornadas foram derivadas da especificação fornecida na tarefa de construção do MVP; não houve arquivo de plano externo.

## Jornadas verificadas

- Como estudante, quero cadastrar uma atividade válida para enxergá-la no quadro.
- Como estudante, quero entender por que uma atividade é prioritária.
- Como estudante, quero saber quais compromissos excedem minha capacidade semanal.
- Como estudante, quero mover, editar e excluir atividades sem acoplar a regra à interface.
- Como estudante, quero revisar o quadro em desktop ou mobile, inclusive por teclado.

## Ciclo RED → GREEN

| Etapa | Evidência |
|---|---|
| RED | `dotnet test CabeNaSemana.slnx --no-restore` falhou por referências intencionalmente ausentes a `StudyTask`, `PriorityEngine`, `WeeklyCapacityPlanner`, `BoardService` e persistência. O scaffold compilava; a falha era a implementação ainda inexistente. |
| GREEN inicial | A mesma suíte compilou e expôs dois defeitos: texto de prazo divergente e HTTP 500 causado pelo `RangeAttribute` decimal sob cultura local. |
| GREEN corrigido | Após ajustar a explicação e usar o overload numérico independente de cultura, 19/19 testes passaram. |
| Refino | Casos de atualização, exclusão, capacidade válida/inválida, ausências, antiforgery e mapeamento de cartão foram adicionados antes do gate final; 29/29 passaram. |
| Docker/PostgreSQL | Testes de seleção do provider e health check falharam antes da implementação; depois, 33/33 passaram e a pilha real persistiu cinco tarefas no PostgreSQL. |

## Especificação executável

| # | Garantia | Alvo | Tipo |
|---|---|---|---|
| 1 | Prazo mais próximo aumenta a prioridade com os demais fatores iguais | `PriorityEngineTests` | unidade |
| 2 | Importância crítica supera importância baixa | `PriorityEngineTests` | unidade |
| 3 | Prazo vencido é identificado e explicado | `PriorityEngineTests` | unidade |
| 4 | Trabalho concluído não disputa prioridade ativa | `PriorityEngineTests` | unidade |
| 5 | Trabalho de menor prioridade que cruza o limite recebe overflow | `WeeklyCapacityPlannerTests` | unidade |
| 6 | Planejamento e concluído não consomem capacidade semanal | `WeeklyCapacityPlannerTests` | unidade |
| 7 | Título, esforço, coluna e capacidade inválidos são rejeitados | `StudyTaskTests`, `WeeklyCapacityPlannerTests` | unidade |
| 8 | Movimento altera coluna e timestamp | `StudyTaskTests`, `BoardServiceTests` | unidade/aplicação |
| 9 | Criar, editar, excluir e ajustar capacidade persistem pelo caso de uso | `BoardServiceTests` | aplicação |
| 10 | Tarefa e capacidade fazem round-trip em SQLite real em memória | `EfBoardRepositoryTests` | integração |
| 11 | A raiz MVC renderiza as quatro colunas | `BoardPageTests` | integração HTTP |
| 12 | Criar, validar, mover e editar funcionam no navegador real | inspeção local em 1440×900 e 390×844 | ponta a ponta |

## Comandos e resultados

```text
dotnet test CabeNaSemana.slnx --no-restore
GREEN: 22/22 testes (checkpoint intermediário)

dotnet test CabeNaSemana.slnx --collect:"XPlat Code Coverage" ...
GREEN: 29/29; cobertura final Domain + Application: 94,42% linhas, 88,42% branches

RED Docker/PostgreSQL: `AddPlannerDatabase` inexistente impediu a compilação dos novos testes

GREEN Docker/PostgreSQL: 33/33; imagem construída, dois containers saudáveis e consulta real retornando cinco tarefas

dotnet list CabeNaSemana.slnx package --vulnerable --include-transitive
GREEN: nenhum pacote vulnerável reportado após override seguro do SQLite nativo
```

Build Release e `dotnet format --verify-no-changes` também passaram sem warnings ou alterações pendentes de formatação.

## Inspeção ponta a ponta

- Desktop: 1440×900, quadro em quatro colunas, sem erro de console.
- Mobile: 390×844, largura de conteúdo igual ao viewport, formulário e cartões em uma coluna.
- Criação: atividade apareceu no quadro e o total de cartões subiu de 5 para 6.
- Movimento: atividade mudou de **Esta semana** para **Em andamento** e seu estado de capacidade foi recalculado.
- Edição: esforço mudou de 2,5 h para 1,5 h e o cartão refletiu `1,5 h`.
- Validação: título vazio e esforço zero retornaram mensagens específicas; o foco foi para `NewTask_Title` com `aria-invalid=true`.

## Lacunas intencionais

- A confirmação visual de exclusão não foi acionada no navegador para não apagar dados durante a inspeção; exclusão está coberta no serviço de aplicação.
- Não há matriz automatizada de múltiplos navegadores. O teste visual usou o navegador embutido baseado em Chromium.
