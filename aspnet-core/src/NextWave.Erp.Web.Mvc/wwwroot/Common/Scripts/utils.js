var app = app || {};
(function () {
  app.utils = app.utils || {};

  app.utils.string = {
    truncate: function (str, maxLength, postfix) {
      if (!str || !maxLength || str.length <= maxLength) {
        return str;
      }

      if (postfix === false) {
        return str.substr(0, maxLength);
      }

      return str.substr(0, maxLength - 1) + '&#133;';
    },
  };

  app.utils.date = {
    containsTime: function (date) {
      if (!date) {
        return false;
      }

      return date.indexOf(':') !== -1;
    },

    getEndOfDay: function (date) {
      if (!date || !moment) {
        return null;
      }

      return moment(date).endOf('day');
    },

    getEndOfDayIfTimeNotExists: function (date) {
      if (this.containsTime(date)) {
        return date;
      }

      return this.getEndOfDay(date);
    },

    formatAsLongDateTime: function (date) {
      return moment(date).format('YYYY-MM-DDTHH:mm:ss.SSS[Z]');
    },
  };

  app.utils.validation = {
    // Adds a custom validation method to jQuery validator
    addValidationMethod: function (name, regexDescriptionKey) {
      $.validator.addMethod(
        name,
        function (value, element, regexpr) {
          if (!regexpr) {
            return false;
          }
          return regexpr.test(value);
        },
        app.localize(regexDescriptionKey)
      );
    },

    // Retrieves a regex pattern from an input field in the given form
    getRegexFromInput: function (form, inputName) {
      if (!form) {
        return null;
      }
      const input = form.find(`input[name=${inputName}]`);
      return input.length ? new RegExp(input.attr('regex')) : null;
    },

    // Dynamically generates validation rules from input field regex patterns
    generateValidationRules: function (form, fields) {
      return fields.reduce((acc, field) => {
        const regex = this.getRegexFromInput(form, field);
        if (regex) {
          acc[field] = {
            [`${field.charAt(0).toLowerCase()}${field.slice(1)}Regex`]: regex,
          };
        }
        return acc;
      }, {});
    },
  };
})();
