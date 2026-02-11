// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Pjax: загрузка контента по ссылкам меню без полной перезагрузки страницы
(function () {
    var container = document.getElementById('main-container');
    var navLinks = document.querySelectorAll('.navbar a[href].nav-link, .navbar a[href].navbar-brand');
    
    if (!container || !navLinks.length) return;
    
    function isSameOrigin(href) {
        try {
            var a = document.createElement('a');
            a.href = href;
            return a.origin === window.location.origin;
        } catch (e) { return false; }
    }
    
    function loadPage(url) {
        fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (r) { return r.text(); })
            .then(function (html) {
                var parser = new DOMParser();
                var doc = parser.parseFromString(html, 'text/html');
                var newContainer = doc.getElementById('main-container');
                if (!newContainer) return;
                
                container.className = newContainer.className;
                container.innerHTML = newContainer.innerHTML;
                document.title = doc.title;
                history.pushState({}, '', url);
                
                // Удаляем старые скрипты страницы (добавленные через pjax), добавляем новые
                document.querySelectorAll('script[data-pjax-page]').forEach(function (s) { s.remove(); });
                doc.querySelectorAll('body script:not([src])').forEach(function (script) {
                    var s = document.createElement('script');
                    s.textContent = script.textContent;
                    s.setAttribute('data-pjax-page', '1');
                    document.body.appendChild(s);
                });
                
                // Обновляем активные ссылки в меню
                var currentUrl = window.location.href;
                navLinks.forEach(function (link) {
                    link.classList.toggle('active', link.href === currentUrl);
                });
            })
            .catch(function () { window.location.href = url; });
    }
    
    navLinks.forEach(function (link) {
        link.addEventListener('click', function (e) {
            var href = link.getAttribute('href');
            if (!href || href === '#' || link.target === '_blank') return;
            if (!isSameOrigin(href)) return;
            if (e.ctrlKey || e.metaKey || e.shiftKey) return;
            
            e.preventDefault();
            loadPage(href);
        });
    });
    
    window.addEventListener('popstate', function () { location.reload(); });
})();

// Сохраняем и восстанавливаем состояние навигационного меню
(function () {
    var STORAGE_KEY = 'quizapp-navbar-expanded';
    var collapseEl = document.querySelector('.navbar-collapse');
    var toggler = document.querySelector('.navbar-toggler');
    
    if (!collapseEl || !toggler) return;
    
    function isExpanded() { return collapseEl.classList.contains('show'); }
    function saveState() {
        try { localStorage.setItem(STORAGE_KEY, isExpanded() ? '1' : '0'); } catch (e) {}
    }
    
    function restoreState() {
        try {
            if (localStorage.getItem(STORAGE_KEY) === '1' && toggler.offsetParent !== null) {
                var bsCollapse = bootstrap.Collapse.getInstance(collapseEl) || new bootstrap.Collapse(collapseEl, { toggle: false });
                bsCollapse.show();
                toggler.setAttribute('aria-expanded', 'true');
            }
        } catch (e) {}
    }
    
    collapseEl.addEventListener('show.bs.collapse', saveState);
    collapseEl.addEventListener('hide.bs.collapse', saveState);
    
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', restoreState);
    } else {
        restoreState();
    }
})();
