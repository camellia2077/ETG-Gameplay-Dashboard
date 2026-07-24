# Dashboard UI Theme

The command-panel UI theme is defined centrally in `src/EtgGameplayDashboard/Ui/DashboardTheme.cs`.

Use these references for theme work:

- [Theme Rules](./ui-theme-rules.md): how `Primary`, `Secondary`, `Outline`, ordinary buttons, category buttons, text, rows, and borders are used.
- [Theme Catalog](./ui-theme-catalog.md): built-in theme IDs, display names, core HEX values, and catalog maintenance notes.

The stable theme ID is stored in configuration. Display names, HEX values, and derived colors remain in plugin source.

Command-result notifications are an exception: their green (success), red (failure), yellow (warning), and blue (information) backgrounds are fixed semantic colors and do not change with the selected theme. Notifications use a single pure-color background without a border. Each also has a visible status marker so it is not identified by color alone.
