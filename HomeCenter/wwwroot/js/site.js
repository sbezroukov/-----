// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// ============================================================================
// Глобальный индикатор загрузки
// ============================================================================
(function () {
    var loadingOverlay = document.getElementById('global-loading-overlay');
    if (!loadingOverlay) return;
    
    var activeRequests = 0;
    var showTimeout = null;
    var minDisplayTime = 300; // Минимальное время показа (мс)
    var showDelay = 200; // Задержка перед показом (мс)
    var shownAt = null;
    
    window.LoadingIndicator = {
        show: function(immediate) {
            activeRequests++;
            if (activeRequests === 1) {
                if (immediate) {
                    loadingOverlay.style.display = 'flex';
                    shownAt = Date.now();
                } else {
                    // Показываем с задержкой, чтобы не мигать на быстрых запросах
                    showTimeout = setTimeout(function() {
                        loadingOverlay.style.display = 'flex';
                        shownAt = Date.now();
                    }, showDelay);
                }
            }
        },
        
        hide: function() {
            activeRequests = Math.max(0, activeRequests - 1);
            if (activeRequests === 0) {
                if (showTimeout) {
                    clearTimeout(showTimeout);
                    showTimeout = null;
                }
                
                // Если индикатор показан, держим его минимальное время
                if (shownAt) {
                    var elapsed = Date.now() - shownAt;
                    var remaining = Math.max(0, minDisplayTime - elapsed);
                    setTimeout(function() {
                        loadingOverlay.style.display = 'none';
                        shownAt = null;
                    }, remaining);
                } else {
                    loadingOverlay.style.display = 'none';
                }
            }
        },
        
        reset: function() {
            activeRequests = 0;
            if (showTimeout) {
                clearTimeout(showTimeout);
                showTimeout = null;
            }
            loadingOverlay.style.display = 'none';
            shownAt = null;
        }
    };
    
    // Автоматический перехват всех fetch запросов
    var originalFetch = window.fetch;
    window.fetch = function() {
        var args = arguments;
        var url = args[0];
        
        // Не показываем индикатор для некоторых запросов
        var skipLoading = false;
        if (args[1] && args[1].headers) {
            var headers = args[1].headers;
            skipLoading = headers['X-Skip-Loading'] === 'true';
        }
        
        if (!skipLoading) {
            LoadingIndicator.show();
        }
        
        return originalFetch.apply(this, args)
            .then(function(response) {
                if (!skipLoading) {
                    LoadingIndicator.hide();
                }
                return response;
            })
            .catch(function(error) {
                if (!skipLoading) {
                    LoadingIndicator.hide();
                }
                throw error;
            });
    };
    
    // Автоматический перехват jQuery AJAX (если используется)
    if (window.jQuery) {
        jQuery(document).ajaxStart(function() {
            LoadingIndicator.show();
        });
        jQuery(document).ajaxStop(function() {
            LoadingIndicator.hide();
        });
        jQuery(document).ajaxError(function() {
            LoadingIndicator.hide();
        });
    }
    
    // Показываем индикатор при отправке форм
    document.addEventListener('submit', function(e) {
        var form = e.target;
        
        // Пропускаем формы с data-no-loading
        if (form.hasAttribute('data-no-loading')) return;
        
        // Пропускаем формы с target="_blank"
        if (form.target === '_blank') return;
        
        LoadingIndicator.show(true);
        
        // Скрываем через 10 секунд на случай если что-то пошло не так
        setTimeout(function() {
            LoadingIndicator.reset();
        }, 10000);
    });
    
    // Скрываем индикатор при загрузке страницы
    window.addEventListener('load', function() {
        LoadingIndicator.reset();
    });
    
    // Скрываем при переходе назад/вперед
    window.addEventListener('pageshow', function() {
        LoadingIndicator.reset();
    });
})();

// ============================================================================
// Pjax: загрузка контента по ссылкам меню без полной перезагрузки страницы
// ============================================================================
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
        // Индикатор загрузки уже показывается автоматически через fetch перехват
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
            .catch(function () { 
                if (window.LoadingIndicator) window.LoadingIndicator.reset();
                window.location.href = url; 
            });
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
    var STORAGE_KEY = 'homecenter-navbar-expanded';
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
