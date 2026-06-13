// Global TomSelect initialization for all .select2 elements
document.addEventListener('DOMContentLoaded', function () {
  if (typeof TomSelect === 'undefined') return;

  document.querySelectorAll('select.select2').forEach(function (el) {
    if (el.tomselect) return; // already initialized
    new TomSelect(el, {
      create: false,
      allowEmptyOption: true,
      render: {
        no_results: function () {
          return '<div class="no-results">Sonuç bulunamadı.</div>';
        }
      }
    });
  });
});
