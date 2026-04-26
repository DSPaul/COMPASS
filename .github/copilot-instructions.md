# Copilot Instructions

## Project Guidelines
- Avalonia the entire Avalonia API can be found at https://docs.avaloniaui.net/api/. Reference it whenever working on Avalonia-related code.

## Code Style
- Variables should have proper, self-descriptive names. You should be able to tell what something is by reading its name. Avoid generic names like `resolved`, `data`, `provider`, `first`, `list`, `root`, `src`, when more descriptive alternatives exist (e.g. `dropTarget`, `dragData`, `dataProvider`, `directParent`, `outermostOwner`).
- For variable naming with collections: when the variable represents a non-generic/untyped collection reference (like `IList`) where the container itself matters, using "List" or "Collection" in the name is acceptable (e.g. `TargetList`, `SourceList`). For domain-level typed variables where you're thinking about the contents, use plural form instead (e.g. `siblings` not `siblingList`, `children` not `childList`, `tags` not `tagList`).
- Use while loops for visual tree walks and similar iterator patterns. For loops should only be used for counting up or down.
- Avoid tuple/record destructuring (e.g. `var (a, b, c) = result;`) because it breaks Find All References on the properties. Use `result.PropertyName` directly instead.