## Agent skills

### Issues and pull requests

Li and Ban decide Issues during their meetings. Agents may read existing Issues, but every Issue write requires an explicit user request for that action. Never automatically create Issues or post claim, release, or progress comments, including when a generic skill asks to publish to an issue tracker.

When the user asks to publish work, push a feature branch and create or update a ready-for-review PR. Li and Ban decide whether to merge or close it. See `Docs/agents/issue-tracker.md`.

### Domain docs

HongMao uses a single project context: `CONTEXT.md` and `Docs/adr/`. See `Docs/agents/domain.md`.

### Push notes

Before each GitHub push, add a dated entry to `Docs/CHANGELOG.md` summarizing the changes and validation for that push. Include the entry in the pushed commit.

### Unity MCP connection

Before Unity feature implementation or debugging, check whether Unity MCP tools are callable for this project and verify the connection with a read-only Editor request, such as the active scene hierarchy or Console. Configuration and server `ready` status alone do not establish an Editor connection. Tell the user whether the probe succeeded. If it fails, explain the limitation and continue with project files and Unity CLI validation where appropriate; identify any Editor-only checks still needed.
