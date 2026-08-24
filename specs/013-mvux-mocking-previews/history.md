# 013 — Historique des versions et décisions

Reconstruction après la perte du workspace ACO (`devid-feat-uno-extensions-architecture`, détruit avec la branche `dev/devid/spec-013-mvux-mocking` non poussée). Sources du merge :
- fichiers **recovery** locaux (reconstruits depuis les transcripts) — portaient la question ouverte « context scope » et la référence au commit `cd4c9ad` ;
- fichiers **VS Code de David** (joints le 23/08 18:22) — `spec.md`/`impl.md` = état v1 (`8d589d9`, non rechargés), `archi.md` = état le plus récent (v4, post-`2618def`) ;
- transcript Telegram complet de la discussion.

---

## Phase 0 — Cadrage (sam. 22/08, soirée)

**Contexte.** Objectif posé par David : helpers de mocking pour les previews UI (Hot Design) et le testing d'apps consommant des feeds (simuler les états des feeds, pas tester les feeds). Deux POCs existants : PR **#3148** (Nick, spec 009 — XAML only, enveloppe POCO/JSON coercée dans `FeedView.Source`) et PR **#3147** (Steve, spec 012 — vocabulaire `Mocks` + générateur `{Vm}Mocks`/`CreateMock`). Vision à 3 niveaux de David : (1) statique dans le XAML, (2) structures de mock par-feed d'un VM, (3) helpers « modèle complet ».

**Discussion & décisions :**
- Mon premier retour (socle 3147 + markup extension + catalogue) recadré par David : partir de **SON design** — `MessageEntry` pour la couche 1 (pas d'enveloppe magique, JSON→dynamic) et le **SwapFeed du hot-reload** pour la couche 2 (contrôle total, système 100 % malléable ; la couche 3 ne devient que des helpers au-dessus).
- Faisabilité vérifiée dans le code : `MessageEntry<T>`/`IMessageEntry` publics ; `MessageEntry.Empty` force l'axe Data → tue le canari « Undefined » (spec 012 §10.2) ; `HotSwapFeed`/`IHotSwapState`/`StateImpl` = seam existant (seul appelant : hot-reload) ; gate `HotReloadSupport.State` ; commandes non swap-backed (gap identifié).
- Construction du VM : ni « vrai VM via DI » ni « ctor sans modèle » → **vrai VM + vrai Model**, services **null-injectés**, prouvé sûr par **analyse de dépendances au codegen** (« option 2 » de David). Fondement : les feeds MVUX sont des arrow-getters lazy (service capturé en closure, touché à l'énumération seulement) ; cas bloquant = accès service **eager dans le ctor**. « On ne contrôle pas comment nos users utilisent notre archi » → l'analyse + diagnostics sont obligatoires.
- Cross-assembly : le gen ne voit que les métadonnées d'un Model d'une autre assembly → accepté à ce stade : *mocking == assembly du Model* (contrats `MyModel.Empty`/`Create(<feeds>)` dispo seulement là).
- **Draft 1** écrit dans le repo (jamais committé, écrasé par le pivot v1) : tier-1 = DTO `FeedMock` + markup `{mvux:Mock}` ; tier-2 = `{Vm}Mocks`/`CreateMock` générés dans l'assembly du Model ; D1–D4 ouvertes.

## v1 — commit `8d589d9` (dim. 23/08 10:42) — le pivot « génération extérieure » + checkpoint

**Discussion (23/08 matin) :** David réalise en review que le mocking doit être **consommable de l'extérieur** (projet de test qui référence l'app) → on ne peut pas injecter le code dans le VM/Model ; le gen MVUX ajoute des **hooks cachés** (sur le modèle de HR) et le gen de mocking prend le contrôle depuis l'extérieur. Son dump : `RecipeModelMock` record `required init` + `Empty`, `Create()`/`Create(steps)` (null-inject + `SetModel`), `SetModel` ≈ `__Reactive_UpdateModel`. Mes vérifications ont ajouté :
- `__Reactive_UpdateModel` inutilisable tel quel (réassigne `__reactiveModel`, `Unsafe.As` sur type étranger = UB) → **méthode dédiée cachée** (confirmé par David, pt 3).
- **Dérivés doivent survivre** (pt « c'est tout le concept ») → découverte de l'ancrage : les feeds sont cachés par `AttachedProperty.GetOrCreate` avec identité stable → **wrap `HotSwapFeed` au niveau du cache Model-feed** ; les dérivations composent sur le wrapper → le swap traverse la logique métier ; `SetModel` = swaps typés, **plus de `dynamic`**.
- **Attributs de dépendances** émis par l'analyse ET déclarables à la main (idée `[FeedShape(...)]` de David, renommée `[FeedDependency]`/`[CtorDependency]`) — nécessaires car le gen externe n'a pas les syntax trees.
- **Instrumentation des ctors** (idée David) : accès service direct dans le ctor → `Create` exige le service en paramètre.
- Non-AOT accepté (pt 4) : mocking = injection dynamique, dev/test only.
- D1–D7 loggées ; D3 (façade) et D4 (flag dédié) tranchées par David.

**Checkpoint demandé par David** : branche `dev/devid/spec-013-mvux-mocking` depuis `main@32faf32`, commit `8d589d9` (3 volets, 293 lignes).

## v2 — commit `292fb5f` (10:47) — review David

- **Dérivés overridables** : `{Model}Mock.StepsCount` nullable comme `Save` — non défini → vraie dérivation sur les inputs swappés ; défini → remplacé (utile pour les tests).
- **Tier 1 sans `FeedMock`** : « je ne vois pas l'intérêt d'un FeedMock à cet endroit » → `FeedView.Source` prend un **`MessageEntry` authorable** non-générique (le concept du framework lui-même).
- **Exemples XAML exigés** et ajoutés (loading pinné, POCO inline, error/empty/undefined en resources, state picker).
- **Contrat d'évolution naturelle** : changer l'instance dans `Source` **pousse** l'entry dans le wrapper existant (diff d'axes vs entry précédente) — interdiction de repasser par un loading state ; recréer le wrapper toléré seulement sans reset visible.
- Purge des références « v1/v2 » dans les documents (« on est en train de l'écrire cette spec, y'a pas de version qui existe »).

## v3 — commit `2618def` (11:08) — review David

- **Axes custom first-class** : une force de MVUX = l'extensibilité par axes → collection `Axes` (`AxisValue`) + `Set(MessageAxis, value)` en code ; identifier XAML résolu contre axes core + enregistrés, inconnu → diagnostic ; les axes custom participent au diff du wrapper.
- **Exemple JsonConverter** demandé (JSON → target object, type via `ConverterParameter` car la DP est `object`) et ajouté.
- Mes deux déductions de l'époque — `MessageEntry` en `DependencyObject` dans `.UI` (pour binder dans `Data`) et push sur mutation de DP — **seront annulées en v4**.

## v4 — révisions de David dans VS Code (commit `cd4c9ad`, perdu ; contenu = son `archi.md` joint)

Réponses de David à ma question « OK avec ce découpage ? » — par édition directe de l'architecture :
- **`MessageEntry` reste un plain CLR object dans Core** — délibérément **PAS** un `DependencyObject` (aucune complexité property-system UI dans le message model).
- **L'entry n'est pas observable** : muter `Data`/`Error`/`IsProgress`/`Axes` après assignation ne pousse rien ; **remplacer l'instance** est l'unité de changement.
- Le converter JSON **n'est plus un livrable** : illustration **app-owned** attachée à `FeedView.Source`, doit retourner `IMessageEntry` ; la spec ne définit ni n'implémente de converter.
- Contrainte explicite : **tiers 2/3 strictement typés de bout en bout** — jamais de `MessageEntry`/enveloppe untyped dans leurs contrats ; le gen émet des types « external, generic and strongly typed ».
- `AxisValue.Axis` typé `string` (identifier XAML) ; l'instance `MessageAxis` typée passe par `Set(...)` en code.
- (Ses `spec.md`/`impl.md` joints = v1 `8d589d9`, non rechargés dans VS Code ; seule l'archi portait la v4 → les volets spec/impl restaurés remontent ces décisions.)

## v5 — dernier échange avant la perte (non committé) — question OUVERTE

- David : le scope **VM** de tier-2 est peut-être accidentel ; la vraie frontière serait le **contexte** qui possède states/subscriptions (**`SourceContext`**, à vérifier en source).
- Syntaxe visée : `using (MockingService.Enable()) { var model = new MyModel(...); }`
- Sémantique à trancher par **spike** (P0-e) : ambient `AsyncLocal` vs global, scopes imbriqués, concurrence, contexte eager vs lazy, survie des contextes après `Dispose`, interaction avec le flag mockable (D4).
- **Aucune réponse acceptée tant que non vérifiée en source et reviewée par David.**

Puis : **perte du workspace ACO** (node détruit, branche non poussée — commits `8d589d9`, `292fb5f`, `2618def`, `cd4c9ad` perdus). Reconstruction → restauration dans ce dossier (spawn `ext-mvux-mock`, 23/08 soir).

## v6 — décision de David (dim. 24/08) — activation scopée TRANCHÉE

- **Le `using (MockingService.Enable())` est certain**, ce n'est plus une question ouverte : c'est **lui** qui active le mocking.
- Granularité au choix de l'appelant : une assembly de tests qui veut le mocking « at large » ouvre le scope dans son **assembly init** ; sinon un scope par test.
- **Motif : le `HotSwapFeed` a un coût.** Activation à la demande uniquement — *« on ne veut pas injecter ce feed dans TOUS les feeds d'une app live »*. Hors scope → aucun wrap, le feed brut est caché comme aujourd'hui.
- Reste au spike (P0-e) le **mécanisme seul** (contexte propriétaire, eager/lazy, `AsyncLocal` vs token porté, imbrication, concurrence, survie après `Dispose`, câblage vers le flag D4) — plus la forme de l'API.
- Répercuté dans les 3 volets : spec §13 + G9 + R7 + D10, archi §1/§6/§7, impl §1/§2.2/§6/§7/§8/§9.

---

## Registre final des décisions

| # | Décision | Version |
| --- | --- | --- |
| D1 | Tier-1 = `MessageEntry` authorable non-générique **dans Core**, plain CLR (pas DO), **non observable**, axes core en propriétés directes + axes custom via `Axes`/`Set` ; remplacement d'instance = push dans le wrapper existant (évolution naturelle, pas de loading flash) | v2→v4 |
| D2 | Commandes via seam `??` (pas de swap analog) | v1 |
| D3 | Façade (`SetModel`/setters générés) devant les hooks ; `HotSwapFeed`/handles non publics | v1 |
| D4 | Flag mockable dédié dans `FeedConfiguration` (découplé du hot reload) | v1 |
| D5 | Codegen de mocking **externe** (projet consommateur) ; gen MVUX = analyse + attributs + hooks cachés | v1 |
| D6 | Swap ancré au **cache Model-feed** → les dérivés survivent (non négociable) ; dérivés néanmoins **overridables** individuellement | v1+v2 |
| D7 | Non-AOT du path mocking accepté (dev/test only) | v1 |
| D8 | Converters = illustrations app-owned à `FeedView.Source` (retournent `IMessageEntry`) ; rien d'implémenté par la feature | v4 |
| D9 | Tiers 2/3 strictement typés ; l'objet tier-1 confiné au tier 1 | v4 |
| D10 | **Activation scopée** : `using (MockingService.Enable())` — jamais un switch app-wide ; assembly init possible pour couvrir tout un run. Hors scope → **aucun wrap** (le `HotSwapFeed` coûte, interdit dans une app live). Seul le mécanisme interne reste à établir par le spike P0-e | v6 |
