# Production Ready Checklist

Uygulamayı production'a almadan önce tamamlanması gereken adımlar.

---

## 1. Mock API'den Gerçek API'ye Geçiş

Şu an tüm API çağrıları `psikoAL/mock-db/` klasöründeki sahte verilerle karşılanıyor. Gerçek backend'e geçmek için aşağıdaki 4 adımı uygula — başka hiçbir yerde değişiklik gerekmez.

**Adım 1 — Mock klasörünü sil:**
```
psikoAL/mock-db/   ← bu klasörün tamamını sil
```

**Adım 2 — `native-atomic/src/lib/api.ts` içindeki mock bloğunu sil:**
```typescript
// ⚠️ MOCK — delete this block + psikoAL/mock-db/ folder when connecting real API
if (process.env.EXPO_PUBLIC_APP_ENV === 'mock') {
  const MockAdapter = require('axios-mock-adapter')
  const { setupMocks } = require('../../../mock-db/handlers')
  setupMocks(new MockAdapter(instance, { delayResponse: 600 }))
}
// END MOCK
```

**Adım 3 — `native-atomic/metro.config.js` içindeki mock bloğunu sil:**
```javascript
// MOCK — remove this block when connecting real API (delete psikoAL/mock-db/ folder too)
nativeWindConfig.watchFolders = [
  ...(nativeWindConfig.watchFolders ?? []),
  path.resolve(__dirname, '../mock-db'),
]
// END MOCK
```

**Adım 4 — `.env` dosyasını güncelle:**
```env
# mock → development (ya da production)
EXPO_PUBLIC_APP_ENV=development
EXPO_PUBLIC_API_URL=https://api.psikoal.com   # gerçek backend URL'i
```

Metro cache'ini temizleyerek yeniden başlat:
```bash
npx expo start --tunnel --clear
```

---
