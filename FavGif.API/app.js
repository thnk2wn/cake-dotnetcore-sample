var express = require('express');
var giphyClient = require('giphy-js-sdk-core');

var app = express();
var port = process.env.port || 3000;
var router = express.Router();

router.route('/books')
    .get(function (req, res) {
        var result = {data: "hello"};
        res.json(result);
    });

app.use('/api', router);

app.get('/', function (req, res) {
    res.send('Welcome to the API');
});

app.listen(port, function () {
    console.log('App started on port ' + port);
})