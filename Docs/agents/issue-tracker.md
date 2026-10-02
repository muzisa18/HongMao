# Issue tracker: GitHub

Issues and feature specs for this repository live in GitHub Issues.
Use the `gh` CLI from inside this repository.

- Create: `gh issue create --title "..." --body "..."`
- Read: `gh issue view <number> --comments`
- List: `gh issue list --state open`
- Comment: `gh issue comment <number> --body "..."`
- Close: `gh issue close <number> --comment "..."`

Pull requests are not treated as feature requests during triage.

When a skill says "publish to the issue tracker", create a GitHub Issue.
When it says "fetch the relevant ticket", run `gh issue view <number> --comments`.
