/*
 * VRLBarcode — donanım (klavye emülasyonlu) barkod okuyucu yardımcısı.
 *
 * Sorun: Fiziksel barkod okuyucu, barkodu çok hızlı yazıp sonunda Enter gönderir.
 * TomSelect gibi arama kutularında bu Enter, AJAX arama bitmeden dropdown'da o an
 * seçili (eski/yarım) ürünü seçtiriyordu.
 *
 * Çözüm: Karakterler arası süreyi ölçerek donanım okuyucuyu tespit ederiz.
 *   - Okuyucu: karakterler arası genelde <15ms; biz `maxIntervalMs` (varsayılan 50ms) eşiği kullanırız.
 *   - İnsan yazımı: karakter araları büyük olduğundan buffer her tuşta sıfırlanır,
 *     Enter anında `minLength`e ulaşmaz ve hiç devreye girmeyiz → mevcut davranış korunur.
 *
 * Neden document + CAPTURE? TomSelect kendi keydown handler'ını input üzerine bizden
 * önce ekler; aynı eleman üzerinde sonradan eklenen dinleyici (capture olsa bile) ondan
 * sonra çalışır. document üzerinde CAPTURE fazında dinleyerek olayı input'a (TomSelect'e)
 * ulaşmadan yakalar, bir tarama tespit edince stopImmediatePropagation ile yanlış seçimi önleriz.
 */
(function (window, document) {
  'use strict';

  function perfNow() {
    return (window.performance && performance.now) ? performance.now() : Date.now();
  }

  function attachKeyboardWedge(inputEl, opts) {
    if (!inputEl) return;
    opts = opts || {};
    var minLength = opts.minLength || 4;
    var maxInterval = opts.maxIntervalMs || 50;
    var onScan = typeof opts.onScan === 'function' ? opts.onScan : function () {};

    var buffer = '';
    var lastTime = 0;

    document.addEventListener('keydown', function (e) {
      if (e.target !== inputEl) return;
      var t = perfNow();

      if (e.key === 'Enter') {
        var code = buffer;
        buffer = '';
        lastTime = t;
        if (code.length >= minLength) {
          // Hızlı seri + Enter → donanım okuyucu taraması.
          e.preventDefault();
          e.stopImmediatePropagation();
          onScan(code);
        }
        return;
      }

      // Yalnızca tek karakterlik (yazılabilir) tuşları biriktir.
      if (e.key && e.key.length === 1) {
        if (buffer && (t - lastTime) > maxInterval) {
          buffer = '';   // önceki tuş yavaştı (insan) → yeni seri başlat
        }
        buffer += e.key;
        lastTime = t;
      }
    }, true);
  }

  // Tam barkod araması: bulunursa ürün nesnesi, bulunamazsa null döner.
  function lookup(url, code) {
    var sep = url.indexOf('?') === -1 ? '?' : '&';
    return fetch(url + sep + 'barcode=' + encodeURIComponent(code))
      .then(function (r) { return r.ok ? r.json() : null; })
      .catch(function () { return null; });
  }

  window.VRLBarcode = { attachKeyboardWedge: attachKeyboardWedge, lookup: lookup };
})(window, document);
