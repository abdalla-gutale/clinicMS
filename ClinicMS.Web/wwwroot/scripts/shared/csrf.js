// Every POST/PUT/DELETE/PATCH endpoint in the app is now protected by ASP.NET Core's
// AutoValidateAntiforgeryTokenAttribute (see Program.cs), which requires the token on every
// unsafe-method request. Feature scripts all call the plain fetch() API without knowing about
// CSRF, so rather than editing every one of them, this wraps window.fetch once, globally, and
// attaches the token header automatically to every non-GET/HEAD request.
(function () {
    var tokenMeta = document.querySelector('meta[name="csrf-token"]');
    if (!tokenMeta) {
        return;
    }
    var token = tokenMeta.content;
    var originalFetch = window.fetch;

    window.fetch = function (input, init) {
        init = init || {};
        var method = (init.method || 'GET').toUpperCase();

        if (method !== 'GET' && method !== 'HEAD') {
            if (init.headers instanceof Headers) {
                init.headers.set('X-CSRF-TOKEN', token);
            } else {
                init.headers = Object.assign({}, init.headers, { 'X-CSRF-TOKEN': token });
            }
        }

        return originalFetch(input, init);
    };
})();
