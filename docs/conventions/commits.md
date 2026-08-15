# Commit ve PR Sözleşmesi

> Kanonik kaynak. `psikoal-app` `docs/CONVENTIONS.md` üzerinden buraya link verir.

## Commit mesajı — Conventional Commits

```
<tür>(<kapsam>): <konu>

[gövde]
[footer]
```

- **Tür** İngilizce ve küçük harf: `feat` `fix` `hotfix` `chore` `infra` `docs` `style`
  `refactor` `perf` `test` `build` `ci` `revert`
- **Kapsam** opsiyonel, domain adı: `offer`, `listing`, `match`, `auth`, `admin`, `ci`
- **Konu** Türkçe yazılabilir, nokta ile bitmez, başlık ≤ 100 karakter
- Yasak mesajlar: `son`, `fix`, `update`, `asdf`, `wip`, `düzeltme`

`psikoal-app` reposunda bu kural `commitlint` + husky `commit-msg` hook'u ile
lokal olarak zorlanır. Backend'de şimdilik disiplin düzeyindedir.

```
feat(offer): teklif geri çekme akışı eklendi
fix(match): sessionType enum'una yüz_yüze_online eklendi
infra(ci): action'lar SHA'ya pinlendi
```

## Dal adı

```
<tür>/<İŞ-KODU>-<kebab-case>

feat/OFFER-12-teklif-geri-cekme
fix/MATCH-3-session-type-drift
```

Türkçe karakter ve boşluk yasak. İş kodu BÜYÜK harf, domain kısaltmasına bağlanır
(`LISTING-`, `OFFER-`, `MATCH-`, `AUTH-`, `ADMIN-`).

## Atomic commit

Bir commit tek bir mantıksal değişiklik içerir. Biçimlendirme (prettier/dotnet-format)
ve davranış değişikliği **ayrı** commit'lerdir — aksi hâlde diff okunamaz hâle gelir.

## Migration kuralı (backend)

Migration'lar **forward-only ve additive-only** yazılır (expand-contract).
Down script yoktur; mobil istemci haftalarca eski şemayla yaşadığı için kolon/tablo
silme işlemi ayrı bir sürüme ertelenir. Ayrıntı: `docs/devops/rollback.md`.
