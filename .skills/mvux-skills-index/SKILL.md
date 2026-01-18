---
name: mvux-skills-index
description: Index of all MVUX/Reactive Extensions skills for Uno Platform development.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-mvux
---

# MVUX Agent Skills Index

This index provides an overview of all Agent Skills available for MVUX (Model-View-Update-eXtended) development with Uno Platform.

## Skills Overview

| Skill | Description | Use When |
|-------|-------------|----------|
| [mvux-overview](./mvux-overview/SKILL.md) | Understand MVUX architecture | Learning MVUX patterns, comparing to MVVM |
| [mvux-feed-basics](./mvux-feed-basics/SKILL.md) | Create IFeed<T> for async data | Loading data from services, reactive data sources |
| [mvux-feedview](./mvux-feedview/SKILL.md) | Display async data with FeedView | Rendering feeds with loading/error/empty states |
| [mvux-state-basics](./mvux-state-basics/SKILL.md) | Create IState<T> for mutable data | Two-way binding, user input, editable state |
| [mvux-listfeed](./mvux-listfeed/SKILL.md) | Create IListFeed<T> collections | Loading lists, filtering, pagination |
| [mvux-liststate](./mvux-liststate/SKILL.md) | Create IListState<T> mutable collections | Editing collections, selection management |
| [mvux-commands](./mvux-commands/SKILL.md) | Generate and use commands | Button actions, method invocation from UI |
| [mvux-selection](./mvux-selection/SKILL.md) | Implement list selection | Single/multi-selection in ListView/GridView |
| [mvux-pagination](./mvux-pagination/SKILL.md) | Paginate large datasets | Infinite scroll, cursor-based APIs |
| [mvux-messaging](./mvux-messaging/SKILL.md) | Sync states with entity changes | CRUD operations, multi-view sync |
| [mvux-records](./mvux-records/SKILL.md) | Use immutable records | Data model design, key equality |

## Quick Reference by Topic

### Getting Started
1. **mvux-overview** - Understand MVUX concepts
2. **mvux-records** - Design immutable data models
3. **mvux-feed-basics** - Load async data

### Displaying Data
1. **mvux-feedview** - FeedView control for all states
2. **mvux-listfeed** - Display collections

### User Input
1. **mvux-state-basics** - Two-way binding with states
2. **mvux-commands** - Button actions and commands

### Advanced Features
1. **mvux-liststate** - Mutable collections
2. **mvux-selection** - Selection management
3. **mvux-pagination** - Infinite scroll
4. **mvux-messaging** - Entity synchronization

## Prerequisites

All skills require:
- Uno Platform 5.x or later
- .NET 8.0 or later
- `MVUX` in `<UnoFeatures>` property

```xml
<UnoFeatures>MVUX</UnoFeatures>
```

## Core Types Reference

| Type | Purpose | Mutable | Skills |
|------|---------|---------|--------|
| `IFeed<T>` | Async read-only value | No | mvux-feed-basics |
| `IState<T>` | Async read/write value | Yes | mvux-state-basics |
| `IListFeed<T>` | Async read-only collection | No | mvux-listfeed |
| `IListState<T>` | Async read/write collection | Yes | mvux-liststate |
| `FeedView` | Control for feed display | N/A | mvux-feedview |
| `Signal` | Refresh trigger | N/A | mvux-feed-basics |

## Common Patterns

### Simple Data Display
```
Model (IFeed) → FeedView → ValueTemplate
```
Skills: mvux-feed-basics, mvux-feedview

### Form Input
```
Model (IState) → TextBox (TwoWay) → State.Update
```
Skills: mvux-state-basics, mvux-commands

### Master-Detail
```
Model (IListFeed + Selection) → ListView → Detail FeedView
```
Skills: mvux-listfeed, mvux-selection, mvux-feedview

### CRUD Operations
```
Service → EntityMessage → Messenger → ListState (auto-update)
```
Skills: mvux-liststate, mvux-messaging

## Version Information

These skills are designed for:
- Uno.Extensions 5.x
- Uno.Extensions.Reactive 5.x
- CommunityToolkit.Mvvm (included with MVUX)

## Related Skills

For navigation with MVUX, see the `uno-navigation-*` skills in the `.skills` directory.
