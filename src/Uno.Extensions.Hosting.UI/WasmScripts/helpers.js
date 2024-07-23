var Uno;
(function (Uno) {
    var Extensions;
    (function (Extensions) {
        class Hosting {
            static getLocation() {
                return window.location.href;
            }
            static displayMessage(message) {
                return console.log(message);
            }
        }
        Extensions.Hosting = Hosting;
    })(Extensions = Uno.Extensions || (Uno.Extensions = {}));
})(Uno || (Uno = {}));
