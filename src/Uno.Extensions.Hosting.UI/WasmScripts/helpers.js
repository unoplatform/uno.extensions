var Uno;
(function (Uno) {
    var Extensions;
    (function (Extensions) {
        class Hosting {
            static getLocation() {
                return window.location.href;
            }
            
        }
        Extensions.Hosting = Hosting;
    })(Extensions = Uno.Extensions || (Uno.Extensions = {}));
})(Uno || (Uno = {}));
