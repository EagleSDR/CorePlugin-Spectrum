const path = require('path');

module.exports = {
    entry: './src/config.ts',
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                use: 'ts-loader',
                exclude: /node_modules/,
            },
        ],
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js'],
    },
    output: {
        filename: 'boot.js',
        path: path.resolve(__dirname, '../assets/'),
    },
};