(function () {
  function initTableFilter(table) {
    const tbody = table.querySelector('tbody');
    if (!tbody) return;

    const toolbar = table.closest('.card')?.querySelector('.table-toolbar');
    const searchInput = toolbar?.querySelector('.js-table-search');
    const pageSizeSelect = toolbar?.querySelector('.js-table-page-size');
    const rows = Array.from(tbody.querySelectorAll('tr')).filter(row => !row.querySelector('td[colspan]'));
    let filteredRows = rows.slice();
    let currentPage = 1;

    function render() {
      const pageSize = parseInt(pageSizeSelect?.value || '25', 10);
      const totalPages = Math.max(1, Math.ceil(filteredRows.length / pageSize));
      if (currentPage > totalPages) currentPage = totalPages;

      rows.forEach(row => { row.style.display = 'none'; });

      const start = (currentPage - 1) * pageSize;
      filteredRows.slice(start, start + pageSize).forEach(row => {
        row.style.display = '';
      });

      let info = toolbar?.querySelector('.js-table-info');
      if (!info && toolbar) {
        info = document.createElement('div');
        info.className = 'text-body-secondary small js-table-info w-100 mt-2';
        toolbar.appendChild(info);
      }
      if (info) {
        if (filteredRows.length === 0) {
          info.textContent = 'Kayıt bulunamadı.';
        } else {
          const from = start + 1;
          const to = Math.min(start + pageSize, filteredRows.length);
          info.textContent = `${filteredRows.length} kayıttan ${from}-${to} arası gösteriliyor`;
        }
      }
    }

    function applySearch() {
      const term = (searchInput?.value || '').trim().toLowerCase();
      filteredRows = rows.filter(row => row.textContent.toLowerCase().includes(term));
      currentPage = 1;
      render();
    }

    searchInput?.addEventListener('input', applySearch);
    pageSizeSelect?.addEventListener('change', () => {
      currentPage = 1;
      render();
    });

    render();
  }

  document.querySelectorAll('table.js-data-table').forEach(initTableFilter);

  document.querySelectorAll('.js-flash-alert').forEach(alertEl => {
    setTimeout(() => {
      const closeBtn = alertEl.querySelector('.btn-close');
      if (closeBtn) closeBtn.click();
    }, 5000);
  });
})();
