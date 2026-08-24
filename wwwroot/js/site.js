
// Giant Defense UI Upgrade - safe frontend helpers
(function () {
    function ready(fn){ if(document.readyState !== 'loading') fn(); else document.addEventListener('DOMContentLoaded', fn); }
    function toast(message){
        let el = document.querySelector('.toast-ui');
        if(!el){ el = document.createElement('div'); el.className='toast-ui'; document.body.appendChild(el); }
        el.textContent = message;
        el.classList.add('show');
        clearTimeout(el._t);
        el._t = setTimeout(function(){ el.classList.remove('show'); }, 2800);
    }
    window.uiToast = toast;
    ready(function(){
        document.querySelectorAll('table').forEach(function(table, idx){
            if(table.dataset.enhanced === 'true') return;
            const rows = table.querySelectorAll('tbody tr');
            if(rows.length < 6) return;
            table.dataset.enhanced = 'true';
            const wrap = document.createElement('div');
            wrap.className = 'ui-toolbar';
            const label = document.createElement('div');
            label.innerHTML = '<strong>بحث سريع</strong><div class="small-muted">فلترة فورية داخل الجدول الحالي</div>';
            const input = document.createElement('input');
            input.className = 'ui-search';
            input.placeholder = 'اكتب للبحث داخل الجدول...';
            input.setAttribute('aria-label','بحث داخل الجدول');
            input.addEventListener('input', function(){
                const q = input.value.trim().toLowerCase();
                table.querySelectorAll('tbody tr').forEach(function(tr){
                    tr.style.display = tr.textContent.toLowerCase().includes(q) ? '' : 'none';
                });
            });
            wrap.appendChild(label); wrap.appendChild(input);
            table.parentNode.insertBefore(wrap, table);
        });
        document.querySelectorAll('a,button').forEach(function(el){
            const text = (el.textContent || '').trim();
            const isDanger = text.includes('حذف') || text.includes('رفض') || el.classList.contains('btn-danger');
            if(isDanger && !el.dataset.confirmBound){
                el.dataset.confirmBound='true';
                el.addEventListener('click', function(e){
                    if(el.dataset.noConfirm === 'true') return;
                    const ok = confirm('هل أنت متأكد من تنفيذ هذه العملية؟');
                    if(!ok) e.preventDefault();
                });
            }
        });
        document.querySelectorAll('form').forEach(function(form){
            form.addEventListener('submit', function(){
                const btn = form.querySelector('button[type="submit"]:not([data-no-loading])');
                if(btn && !btn.disabled){
                    btn.dataset.oldText = btn.innerHTML;
                    btn.innerHTML = '<span class="spinner-border spinner-border-sm ms-2"></span> جاري التنفيذ...';
                    btn.disabled = true;
                    setTimeout(function(){ if(btn){ btn.disabled=false; btn.innerHTML=btn.dataset.oldText || btn.innerHTML; } }, 8000);
                }
            });
        });
        const alerts = document.querySelectorAll('.alert-success,.alert-info');
        if(alerts.length){ toast(alerts[0].textContent.trim()); }
    });
})();


// FULL UI REDESIGN v5 - universal role enhancements
(function(){
    function ready(fn){document.readyState!=='loading'?fn():document.addEventListener('DOMContentLoaded',fn)}
    ready(function(){
        document.body.classList.add('layout-ready');
        // Add mini page actions when a page lacks obvious navigation buttons
        var main = document.querySelector('.content-body,.admin-content');
        if(main && !main.querySelector('.page-mini-actions') && !main.querySelector('.full-hero')){
            var bar=document.createElement('div');
            bar.className='page-mini-actions';
            bar.innerHTML='<a href="/">🏠 الرئيسية</a><a href="javascript:history.back()">↩ رجوع</a>';
            main.insertBefore(bar, main.firstChild);
        }
        // Improve empty table/list messages
        document.querySelectorAll('div').forEach(function(d){
            var t=(d.textContent||'').trim();
            if((t==='لا توجد اختبارات منشورة حالياً.'||t==='لا توجد بيانات'||t==='لا توجد نتائج') && !d.classList.contains('ui-empty')) d.classList.add('ui-empty');
        });
        // Make table containers responsive automatically
        document.querySelectorAll('table').forEach(function(table){
            if(!table.parentElement.classList.contains('table-responsive')){
                var wrapper=document.createElement('div');
                wrapper.className='table-responsive';
                table.parentNode.insertBefore(wrapper, table);
                wrapper.appendChild(table);
            }
        });
        // Prevent accidental back to edit page after publish by marking return buttons safe only when explicit
        document.querySelectorAll('a[href*="/Teacher/Exams/Edit"]').forEach(function(a){
            if((a.textContent||'').includes('رجوع') || (a.textContent||'').includes('عودة')) a.setAttribute('href','/Teacher/Exams');
        });
    });
})();
