var Cedita = (!Cedita ? function () { } : Cedita);
if (!Cedita.Labs) Cedita.Labs = function () { };

/**
 * WC Logger
 * 
 * Built to replicate the 'tail' functionality of a log, with the ability to start / stop the log monitoring
 * 
 * @code
 * (new WC.Logger())
 *       .setLogUrl('/cron/log/raw')
 *       .setTarget('cron_log')
 *       .enableIcons(true)
 *       .init();
 * 
 * @copyright Cedita Digital Ltd
 */
Cedita.Labs.Logger = function () {

    /** @var string URL to request log entries */
    this.log_url = null;

    /** @var int Interval between 'refreshes' */
    this.interval = 1000;

    /** @var string Target for our log output */
    this.target = 'log';

    /** @var string Interval instance */
    this.request = null;

    /** @var bool Enable / Disable parsing of log for level icons */
    this.level_icons = false;

    /** @var int Pointer at which our last log file was at */
    this.pointer = null;

    /** @var array Regular expression to apply highlighting */
    this.highlighter = {
        '(\\[\\d+-\\d+-\\d+ \\d+:\\d+:\\d+\\])': '<span class="yellow text-bold">$1</span>'
    };

    /** @var bool Apply highlighter */
    this.highlight = false;

    /** @var bool Flag to auto scroll if we are at the bottom of the div */
    this.auto_scroll = true;

    /** @var bool Enable search filtering */
    this.search_enabled = false;

    /** @var array Stores the request data to be used on each request */
    this.requestData = {};

    /** @var int Selected instance */
    this.instanceId = null;

    /**
     * Sets our log url
     * 
     * @param url
     * @return {*}
     */
    this.setLogUrl = function (url) {
        this.log_url = url;
        return this;
    };

    /**
     * Sets our Interval length
     * 
     * @param interval
     * @return {*}
     */
    this.setInterval = function (interval) {
        this.interval = interval;
        return this;
    };

    /**
     * Do we want to highlight our log?
     * 
     * @param bool
     * @return {*}
     */
    this.setHighlight = function (bool) {
        this.highlight = bool;
        return this;
    };

    /**
     * Enables parsing of our line for warning level icons
     * 
     * @param bool
     * @return {*}
     */
    this.enableIcons = function (bool) {
        this.level_icons = bool;
        return this;
    };

    /**
     * Enables the search facility for a given log
     * 
     * @param {any} bool
     * @return {*}
     */
    this.enableSearch = function (bool) {
        this.search_enabled = bool;
        return this;
    };

    /**
     * Sets our log target
     * 
     * @param target
     * @return {*}
     */
    this.setTarget = function (target) {
        this.target = target;
        return this;
    };

    /**
     * Sets the selected instance id
     * 
     * @param {any} id
     * @return {*}
     */
    this.setInstanceId = function (id) {
        this.instanceId = id;

        // If we have already prepared request data, update this too
        if (this.requestData != null && this.requestData.appId != null) this.requestData.appId = id;

        return this;
    };

    /**
     * Initialises our log, binds our start / stop events
     * 
     * @return {*}
     */
    this.init = function () {
        this.bindEvents();
        return this;
    };

    /**
     * Binds our Start / Stop links
     */
    this.bindEvents = function () {
        var that = this;
        $('.' + this.target + '_stop_trigger').click(function () {
            that.stop();
            return false;
        });

        console.log('.' + this.target + '_start_trigger');
        $('.' + this.target + '_start_trigger').click(function () {
            that.start();
            return false;
        });

        $('.' + this.target + '_clear').click(function () {
            $('#' + that.target).html('');
            return false;
        });
    };

    /**
     * Gets our log from our requested url and processes
     */
    this.getLog = function () {
        var that = this;

        that.requestData = {
            appId: that.instanceId,
            from: that.pointer
        };

        // Have we enabled search?
        if (this.search_enabled) {

            // Reset any search results we currently have
            $('#' + that.target).html('');

            var ctr = $('#' + that.target).prev('.log-container');
            if (ctr.length > 0) {
                that.requestData.from = $(ctr).find('input[name=FromTime]').val();
                that.requestData.to = $(ctr).find('input[name=ToTime]').val();
                that.requestData.filter = $(ctr).find('input[name=MessageFilter]').val();
            }
        }

        this.request = setInterval(function () {
            $.ajax({
                url: that.log_url,
                data: that.requestData,
                dataType: 'json'
            }).done(function (response) {

                // Set our pointer
                if (response.pointer != null) {
                    that.requestData.from = response.pointer;
                }

                var log_output = '';
                if (response.logs != null) {

                    if (response.logs.length == 0 && that.search_enabled) {
                        $('#' + that.target).append('[' + moment().format('YYYY-MM-DD HH:mm:ss') + '] No results found');
                    }

                    for (i = 0; i < response.logs.length; i++) {
                        if (response.logs[i].length == 0) {
                            continue;
                        }

                        var timestamp = new moment(response.logs[i].item2);
                        var level = response.logs[i].item4;
                        var line = "[" + timestamp.format('YYYY-MM-DD HH:mm:ss') + "] [<span class='log-level-" + that.cleanClassName(level) + "'>" + level + "</span>] " + response.logs[i].item3;

                        if (that.highlight) {
                            for (var regexp in that.highlighter) {
                                line = line.replace(new RegExp(regexp), that.highlighter[regexp]);
                            }
                            // Replace new lines separately
                            line = line.replace(/\\n/g, '<br />');
                        }

                        if (that.level_icons) {
                            var image = that.getLevelImage(level);
                            log_output += '<p><img src="' + image + '" />' + line + "</p>\n";
                        } else {
                            log_output += line + "<br />";
                        }
                    }
                }

                // If we are very close to the end of our div, enable auto scroll, otherwise don't
                var target = document.getElementById(that.target);
                var enableAutoScroll = false;
                if ((target.clientHeight + target.scrollTop) >= (target.scrollHeight - 50))
                    enableAutoScroll = true;

                // Append our output
                $('#' + that.target).append(log_output);

                if (enableAutoScroll)
                    target.scrollTop = target.scrollHeight;

                }).fail(function (e) {
                    console.log('Error');
                    console.log(e);
                });

            if (that.search_enabled) {
                clearInterval(that.request);
            }

        }, that.interval);

    };

    /**
     * Starts our log monitoring
     * 
     * @return {*}
     */
    this.start = function () {
        $('#' + this.target).removeClass('disabled').removeClass('paused');
        this.getLog();
        return this;
    };

    /**
     * Stops our log monitoring
     * 
     * @return {*}
     */
    this.stop = function () {
        $('#' + this.target).addClass('paused');
        clearInterval(this.request);
        return this;
    };

    /**
     * Parses our log line to extract the warning level, this is compatible with Zend_Log entries only
     * 
     * @param line
     * @return {*}
     */
    this.getLogLevel = function (line) {
        var match = line.match(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\+\d{2}:\d{2}\s([A-Z]+)\s/);
        if (match[1].length) {
            return match[1];
        }
        return false;
    };

    /**
     * Returns an image depending on our log level
     * 
     * @param level
     * @return {String}
     */
    this.getLevelImage = function (level) {
        switch (level) {
            case 'ALERT':
            case 'CRIT':
            case 'ERR':
            case 'Error':
                return 'http://cdn1.iconfinder.com/data/icons/tiny-icons/exclamation.png';
                break;
            case 'Warning':
            case 'WARN':
                return 'http://cdn1.iconfinder.com/data/icons/tiny-icons/warning.png';
                break;
            case 'DEBUG':
            case 'INFO':
            case 'Information':
            case 'Verbose':
                return 'http://cdn1.iconfinder.com/data/icons/tiny-icons/info.png';
                break;
            default:
                return 'http://cdn1.iconfinder.com/data/icons/tiny-icons/info.png';
                break;
        }
    };

    /**
     * Returns a clean css class name for a given string
     * @param {any} classStr
     */
    this.cleanClassName = function (classStr)
    {
        if (classStr == null || typeof classStr === undefined)
            return "";

        return classStr.toLowerCase().replace(" ", "-");
    }
};