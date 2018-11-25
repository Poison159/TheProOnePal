
$(document).ready(function (e) {
    nav.init();
    accordion.init();
    bab.init();
    pagination.init();
});


var html = document.documentElement,
	$html = $(html),
	multiplier,
	current = 'current',
	close = 'close',
	open = 'open',
	selected = 'selected',
	jsNone = 'js_none',
	ariaHidden = 'aria-hidden',
	ariaInvalid = 'aria-invalid',
	ariaDescribedBy = 'aria-describedby';

var nav = {
    init: function () {
        $('#nav_main > ul > li').hoverIntent({
            timeout: 500,
            over: function () {
                $(this).addClass(open);
            },
            out: function () {
                $(this).removeClass(open);
            }
        });
    }
};

var accordion = {
    init: function () {
        var $accordionLinks = $('.accordion > li > a'),
			$accordionAll = $('#accordion-all'),
			accordionAllOpen = $accordionAll.text(),
			accordionAllClose = accordionAllOpen.replace('Show', 'Hide');

        $accordionLinks.on('click', function (e) {
            e.preventDefault();
            $(this).toggleClass(open);
        });

        $accordionAll.on('click', function (e) {
            e.preventDefault();
            var $this = $(this);
            $this.toggleClass(close);

            if ($this.hasClass(close)) {
                $this.text(accordionAllClose);
                $accordionLinks.addClass(open);
            }
            else {
                $this.text(accordionAllOpen);
                $accordionLinks.removeClass(open);
            }
        });
    }
};
