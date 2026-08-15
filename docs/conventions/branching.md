# Dal Modeli ve Terfi Kuralları

> **Bu dosya iki repo için de TEK kanonik kaynaktır.** `psikoal-app` reposundaki
> `docs/CONVENTIONS.md` yalnızca buraya link verir; içerik ikizlenmez (ikizlenen doküman
> senkron kaybeder).

## Dallar

| Repo | Varsayılan dal | Geliştirme dalı |
|---|---|---|
| `MMDProjects/psikoal-app` | `master` | `dev` |
| `MMDProjects/psikoal-backend` | `main` | `dev` |

İki repo **bağımsız** terfi eder. Ortak bir "sürüm" kavramı yoktur; çapraz uyumluluğu
API sözleşme testleri (`PsikoAl.Api.ContractTests`) ve `contracts/` şemaları taşır.

```
feat/<İŞ-KODU>-<kebab>  ──PR (squash)──►  dev  ──PR (merge commit)──►  master / main
hotfix/<İŞ-KODU>-<kebab> ─────────────── PR (merge commit) ──────────►  master / main
```

## Dört terfi kuralı

1. `master`/`main`'e **yalnız** `dev` veya `hotfix/*` dalından PR açılır.
   CI'daki `enforce-promotion` job'u bunu zorlar (branch protection kaynak dalı kısıtlayamaz).
2. İş dalı → `dev` merge'i **squash**; `dev` → `master`/`main` merge'i **merge commit**.
   Terfi dallarında squash YASAKTIR — yeni commit üretir ve dallar kalıcı ayrışır.
3. Prod'da bulunan hata `master`/`main` üzerinde düzeltilmez; `dev`'den `fix/` dalı açılır
   ve normal terfi zincirinden geçer. İstisna: gerçek acil durum → `hotfix/*`.
4. `hotfix/*` `master`/`main`'e merge edildikten sonra **geri-merge zorunludur**
   (`master`/`main` → `dev`). Atlanırsa bir sonraki terfi düzeltmeyi geri alır.

## Dal ömrü

- İş dalı ömrü ≤ 3 gün. Uzarsa rebase edilemez hâle gelir.
- Silinen dallar `archive/<ad>` tag'i olarak korunur.

## Ruleset kurulumu — İKİ FAZLI (önemli)

Hiç koşmamış bir check'i `required` yapmak PR'ları süresiz bloklar.

- **Faz 1 (hemen):** doğrudan push yasağı, merge yöntemi kısıtı, force-push yasağı.
- **Faz 2 (job'lar en az bir kez koştuktan ve gerçek bir FAIL ürettikten sonra):**
  required status checks.

Hiçbir job, **en az bir kez gerçek FAIL üretmeden** ve varsa `continue-on-error: true`
satırı kaldırılmadan required yapılmaz. Aksi hâlde "sahte yeşil kapı" kurulmuş olur.

### Hedef dal başına required check'ler

| Hedef | psikoal-app | psikoal-backend |
|---|---|---|
| `dev` | `quality` | `build-test`, `migration-lint` |
| `master` / `main` | `quality`, `bundle`, `enforce-promotion` | `build-test`, `migration-lint`, `gitleaks`, `enforce-promotion` |

`migration-apply` şu an `continue-on-error: true` ile koşuyor — required DEĞİL.
