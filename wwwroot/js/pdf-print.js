(function () {
  'use strict';

  function buildInlineUrl(url) {
    if (!url) return url;
    if (url.indexOf('inline=') >= 0) return url;
    return url + (url.indexOf('?') >= 0 ? '&' : '?') + 'inline=true';
  }

  function printPdf(url) {
    if (!url) return;

    var printUrl = buildInlineUrl(url);

    fetch(printUrl, { credentials: 'same-origin' })
      .then(function (response) {
        if (!response.ok) {
          throw new Error('PDF yüklenemedi');
        }
        return response.blob();
      })
      .then(function (blob) {
        var blobUrl = URL.createObjectURL(blob);
        var iframe = document.createElement('iframe');
        iframe.setAttribute('aria-hidden', 'true');
        iframe.style.position = 'fixed';
        iframe.style.right = '0';
        iframe.style.bottom = '0';
        iframe.style.width = '0';
        iframe.style.height = '0';
        iframe.style.border = '0';
        iframe.src = blobUrl;

        iframe.onload = function () {
          setTimeout(function () {
            try {
              iframe.contentWindow.focus();
              iframe.contentWindow.print();
            } catch (err) {
              window.open(blobUrl, '_blank');
            }
          }, 300);
        };

        document.body.appendChild(iframe);

        setTimeout(function () {
          URL.revokeObjectURL(blobUrl);
          if (iframe.parentNode) {
            iframe.parentNode.removeChild(iframe);
          }
        }, 60000);
      })
      .catch(function () {
        alert('Yazdırma için PDF yüklenemedi.');
      });
  }

  document.addEventListener('click', function (event) {
    var button = event.target.closest('.js-print-pdf');
    if (!button) return;

    event.preventDefault();
    printPdf(button.getAttribute('data-pdf-url'));
  });

  window.printPdf = printPdf;
})();
