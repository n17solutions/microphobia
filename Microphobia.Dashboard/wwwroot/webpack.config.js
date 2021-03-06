'use-strict';

const webpack = require('webpack');
const path = require('path');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const FaviconsWebpackPlugin = require('favicons-webpack-plugin');

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
      },
        {
         test: /\.(png|svg|jpg|gif)$/,
            use: 'file-loader'
        }
    ]
  },

  resolve: {
    modules: [
      resolve('src'),
      resolve('src/css'),
        resolve('src/images'),
      'node_modules'
    ]
  },

  plugins: [
      new FaviconsWebpackPlugin({
          logo: './src/images/logo.png',
          prefix: './favicons/[hash]-',
          persistentCache: false,
          inject: true,
          title: 'Microphobia'
      }),
      new HtmlWebpackPlugin({
          template: resolve('./src/index.html')
      }),
      new webpack.DefinePlugin({
        'process.env.NODE_ENV': '"production"'
      })
  ]
};
