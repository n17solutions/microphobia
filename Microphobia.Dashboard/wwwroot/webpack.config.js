'use-strict';

const webpack = require('webpack');
const path = require('path');
const HtmlWebpackPlugin = require('html-webpack-plugin');

const resolve = (dir) => path.join(__dirname, dir)

module.exports = {
  mode: 'production',

  entry: {
    microphobia: resolve('./src/index.js')
  },

  output: {
    path: resolve('build/'),
    filename: '[name].min.[hash].js'
  },
    
    resolve: {
      extensions: [".js"]
    },

  optimization: {
    splitChunks: {
      cacheGroups: {
        commons: {
          test: /[\\/]node_modules[\\/]/,
          name: 'vendor',
          chunks: 'all'
        }
      }
    }
  },

  module: {
    rules: [
      {
        test: /\.css$/,
        loader: 'style-loader!css-loader'
      },
      {
        test: /\.html$/,
        use: 'raw-loader'
      },
      {
        test: /^(?!.*\.{test,min}\.js$).*\.js$/,
        exclude: /(node_modules)/,
        use: {
            loader: 'babel-loader'
        }
      }
    ]
  },

  resolve: {
    modules: [
      resolve('src'),
      resolve('src/css'),
      'node_modules'
    ]
  },

  plugins: [
      new HtmlWebpackPlugin({
          template: resolve('./src/index.html')
      }),
      new webpack.DefinePlugin({
        'process.env.NODE_ENV': '"production"'
      })
  ]
};
