var gulp = require('gulp');
var nodemon = require('gulp-nodemon');
var path = require('path');
var minimist = require('minimist');
var fs = require('fs');

var knownOptions = {
    string: 'packageName',
    string: 'packagePath',
    default: {packageName: "Package.zip", packagePath: path.join(__dirname, '_package')}
}

var options = minimist(process.argv.slice(2), knownOptions);

gulp.task('debug', function () {
    nodemon( {
        script: 'app.js',
        ext: 'js',
        env: {
            port: 8000
        },
        ignore: [
            './node_modules/**'
        ]
    })
    .on('restart', function () {
        console.log('Restart detected');
    });
});

gulp.task('package', function () {
    var packagePaths = ['**/*.js', 
                    '**/*.config', 
                    '**/*.json', 
                    '**/views/**/*.*', 
                    '**/public/**/*.*', 
                    '!**/*.spec.js', 
					'!**/_package/**', 
					'!**/typings/**',
                    '!typings', 
                    '!tsconfig.json',
                    '!package*.json',
                    '!_package', 
					'!gulpfile.js'];
	
	// Exclude dev dependencies
	var packageJSON = JSON.parse(fs.readFileSync(path.join(__dirname, 'package.json'), 'utf8'));
	var devDeps = packageJSON.devDependencies;

	for (var propName in devDeps)
	{
		var excludePattern1 = "!**/node_modules/" + propName + "/**";
		var excludePattern2 = "!**/node_modules/" + propName;
		packagePaths.push(excludePattern1);
		packagePaths.push(excludePattern2);
	}
	
    return gulp.src(packagePaths)
        .pipe(gulp.dest(options.packagePath));
});
