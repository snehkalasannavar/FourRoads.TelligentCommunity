(function($, global)
{
    if (typeof $.fourroads === 'undefined')
        $.fourroads = {};

    if (typeof $.fourroads.widgets === 'undefined')
        $.fourroads.widgets = {};

    var attachHandlers = function (context) {
            context.selectors.submit.click(function(){save(context, context.selectors.validateInput.val());});
        },
        scrapeElements = function (context) {
            $.each([context.selectors], function(i, set) {
                $.each(set, function(key, value) {
                    set[key] = $(value);
                });
            });
        },
        save = function(context , code) {
            var data = {
                validationCode: code
            };

            context.selectors.validateInput.closest('.field-item').find('.field-item-validation').hide();

            return $.telligent.evolution.post({
                url: context.urls.validate,
                data: data,
                dataType: 'json',
                success: function(response) {
                    console.log(response);
                    console.log(response.result);
                    if (response.result === "true") {
                        window.location = context.urls.returnUrl;
                    } else {
                        //Show error message
                        context.selectors.validateInput.closest('.field-item').find('.field-item-validation').show();
                    }

                }
            });
        };

    $.fourroads.widgets.mfa = {
        register: function(context) {
            scrapeElements(context);

            attachHandlers(context);
        }
    };
})(jQuery, window);
