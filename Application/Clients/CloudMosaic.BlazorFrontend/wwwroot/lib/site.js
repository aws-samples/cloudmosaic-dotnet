window.cloudMosaicJsFunctions = {

    getFileSize: function (id) {

        var fi = document.getElementById(id);
        if (fi.files.length != 1) {
            return -1;
        }

        return fi.files[0].size;
    }

}