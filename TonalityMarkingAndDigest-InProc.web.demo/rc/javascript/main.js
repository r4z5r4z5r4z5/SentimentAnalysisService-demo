$(document).ready(function () {
    var MAX_INPUTTEXT_LENGTH  = 10000,
        LOCALSTORAGE_TEXT_KEY = 'tonality-marking-text',
        LOCALSTORAGE_BYSENTENCESVIEW_KEY = 'by-sentences-view',
        LOCALSTORAGE_PROCESSTYPE_KEY = 'process-type',
        DEFAULT_TEXT = '«Демократическая» Россия во главе с не менее «демократическим» президентом и его марионетками умело используют во внутренней и внешней политике страны «политику тайной дипломатии», политику обмана россиян, которую начал применять Горбачев еще во времена Перестройки. Перестройка преподносилась советскому народу как забота о нем, забота о предоставлении больших экономических возможностей, как расширение демократических прав и свобод Вся горбачевская фразеология, такая как «углубить», «расширить»,«гласность», «новое мышление», его "гоканья" и "гаканья" раскрывали перед нами не только провинциала, но и политического клоуна, убогого с отметиной на голове, оказавшегося у руля огромной державы Чету Горбачевых, выходцев из Ставропольского края, неожиданно перебравшихся в Кремлевские палаты, но не научившихся даже правильно говорить по-русски, пышно принимали в Великобритании, Франции, США. Головы этих провинциалов не трудно было вскружить прелестями западной жизни и особым вниманием к ним лично со стороны президентов и королей великих держав мира. Как когда-то при колонизации Америки белые люди подкупали вождей краснокожих водкой, бижутерией, разной чепухой взамен на земли индейцев, так и русских Михаила и Раису подкупили бриллиантовыми украшениями,долларами и недвижимостью в предместьях Парижа, прикрываясь риторикой об общечеловеческих ценностях, о демократии, взамен на ключи от великой державы.';

    var textOnChange = function () {
        var _len = $("#text").val().length; 
        var len = _len.toString().replace(/\B(?=(\d{3})+(?!\d))/g, " ");
        var $textLength = $("#textLength");
        $textLength.html("длина текста: " + len + " символов");
        if (MAX_INPUTTEXT_LENGTH < _len) $textLength.addClass("max-inputtext-length");
        else                             $textLength.removeClass("max-inputtext-length");
    };
    var getText = function( $text ) {
        var text = trim_text( $text.val().toString() );
        if (is_text_empty(text)) {
            alert("Введите текст для обработки.");
            $text.focus();
            return (null);
        }
        
        if (text.length > MAX_INPUTTEXT_LENGTH) {
            if (!confirm('Превышен рекомендуемый лимит ' + MAX_INPUTTEXT_LENGTH + ' символов (на ' + (text.length - MAX_INPUTTEXT_LENGTH) + ' символов).\r\nТекст будет обрезан, продолжить?')) {
                return (null);
            }
            text = text.substr( 0, MAX_INPUTTEXT_LENGTH );
            $text.val( text );
            $text.change();
        }
        return (text);
    };

    $("#text").focus(textOnChange).change(textOnChange).keydown(textOnChange).keyup(textOnChange).select(textOnChange).focus();

    (function () {
        function isGooglebot() {
            return (navigator.userAgent.toLowerCase().indexOf('googlebot/') != -1);
        };
        if (isGooglebot())
            return;

        var text = localStorage.getItem(LOCALSTORAGE_TEXT_KEY);
        if (!text || !text.length) {
            text = DEFAULT_TEXT;
        }
        $('#text').val(text).focus();
    })();

    $('#bySentencesView').click(function () {
        var ck = $(this).is(':checked');
        $(this).parent().toggleClass('checked', ck).toggleClass('unchecked', !ck);
        if (ck) {            
            localStorage.removeItem(LOCALSTORAGE_BYSENTENCESVIEW_KEY);
        } else {
            localStorage.setItem(LOCALSTORAGE_BYSENTENCESVIEW_KEY, 'not');
        }
    });
    if (localStorage.getItem(LOCALSTORAGE_BYSENTENCESVIEW_KEY)) {
        $('#bySentencesView').click();
    }
    $('#tonalityMarkingView').click(function () {
        var ck = $(this).is(':checked');
        $('#digestView').prop('checked', !ck);
        $('#bySentencesView').parent().css('visibility', ck ? '' : 'hidden');

        $('label[for="tonalityMarkingView"]').toggleClass('checked', ck).toggleClass('unchecked', !ck);
        $('label[for="digestView"]').toggleClass('checked', !ck).toggleClass('unchecked', ck);
        if (ck) {
            localStorage.removeItem(LOCALSTORAGE_PROCESSTYPE_KEY);
        } else {
            localStorage.setItem(LOCALSTORAGE_PROCESSTYPE_KEY, 'Digest');
        }
    });
    $('#digestView').click(function () {
        var ck = $(this).is(':checked');
        $('#tonalityMarkingView').prop('checked', !ck);
        $('#bySentencesView').parent().css('visibility', !ck ? '' : 'hidden');

        $('label[for="tonalityMarkingView"]').toggleClass('checked', !ck).toggleClass('unchecked', ck);
        $('label[for="digestView"]').toggleClass('checked', ck).toggleClass('unchecked', !ck);
        if (!ck) {
            localStorage.removeItem(LOCALSTORAGE_PROCESSTYPE_KEY);
        } else {
            localStorage.setItem(LOCALSTORAGE_PROCESSTYPE_KEY, 'Digest');
        }
    });
    if (localStorage.getItem(LOCALSTORAGE_PROCESSTYPE_KEY)) {        
        $('#digestView').click();
    }
    $('#resetText2Default').click(function () {
        $("#text").val('');
        setTimeout(function () {
            $("#text").val(DEFAULT_TEXT).focus();
        }, 100);
    });

    $('#mainPageContent').on('click', '#processButton', function () {
        if($(this).hasClass('disabled')) return (false);

        var text = getText( $("#text") );
        if (!text) return (false);

        var bySentencesView = $('#bySentencesView').is(':checked');
        var processType     = $('#digestView').is(':checked') ? 'Digest' : 'TonalityMarking';
        
        processing_start();
        if (text != DEFAULT_TEXT) {
            localStorage.setItem(LOCALSTORAGE_TEXT_KEY, text);
        } else {
            localStorage.removeItem(LOCALSTORAGE_TEXT_KEY);
        }

        $.ajax({
            type: "POST",
            url:  "RESTProcessHandler.ashx",
            timeout: (600 * 1000),
            data: {
                text: text,
                processType: processType,
                splitBySentences: bySentencesView
            },
            success: function (responce) {
                if (responce.error) {
                    if (responce.error == "goto-on-captcha") {
                        window.location.href = "Captcha.aspx";
                    } else {
                        processing_end();
                        $('.result-info').addClass('error').text(responce.error);
                    }
                } else {
                    if (responce.html) {
                        $('.result-info').removeClass('error').text('');
                        $('#processResult').append(responce.html).append('<div style="font-size: x-small; text-align: right; color: gray;">elapsed: ' + responce.elapsed + '</div>');
                        $('.result-info').hide();
                        processing_end();
                    } else {
                        processing_end();
                        $('.result-info').text('значимых сущностей в тексте не найденно');
                    }
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {
                processing_end();
                $('.result-info').addClass('error').text('ошибка сервера');
            }
        });
        
    });

    function processing_start(){
        $('#text').addClass('no-change').attr('readonly', 'readonly').attr('disabled', 'disabled');
        $('.result-info').show().removeClass('error').html('Идет обработка... <label id="processingTickLabel"></label>');
        $('#processButton').addClass('disabled');
        $('#processResult').empty();
        setTimeout(processing_tick, 1000);
    };
    function processing_end(){
        $('#text').removeClass('no-change').removeAttr('readonly').removeAttr('disabled');
        $('.result-info').removeClass('error').text('');
        $('#processButton').removeClass('disabled');
    };
    function trim_text(text) {
        return (text.replace(/(^\s+)|(\s+$)/g, ""));
    };
    function is_text_empty(text) {
        return (text.replace(/(^\s+)|(\s+$)/g, "") == "");
    };
    (function() {
        $.ajax({
            type: "POST",
            url: "RESTProcessHandler.ashx",
            timeout: (600 * 1000),
            data: { text: "_dummy_", forceLoadModel: true }
        });
    })();

    var processingTickCount = 1;
    function processing_tick() {
        var n2 = function (n) {
            n = n.toString();
            return ((n.length == 1) ? ('0' + n) : n);
        }
        var d = new Date(new Date(new Date(new Date().setHours(0)).setMinutes(0)).setSeconds(processingTickCount));
        var t = n2(d.getHours()) + ':' + n2(d.getMinutes()) + ':' + n2(d.getSeconds()); //d.toLocaleTimeString();
        var $s = $('#processingTickLabel');
        if ($s.length) {
            $s.text(t);
            processingTickCount++;
            setTimeout(processing_tick, 1000);            
        } else {
            processingTickCount = 1;
        }        
    }
});