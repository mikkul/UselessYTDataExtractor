function keepScrollPositionOnReload(element) {
    $(element).scroll(function () {
        sessionStorage.scrollTop = $(this).scrollTop();
    });

    $(document).ready(function () {
        if (sessionStorage.scrollTop != "undefined") {
            $(element).scrollTop(sessionStorage.scrollTop);
        }
    });
}