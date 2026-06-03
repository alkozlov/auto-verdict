const prerender = require('prerender');

const server = prerender({
  chromeLocation: '/usr/bin/chromium',
  chromeFlags: [
    '--no-sandbox',
    '--disable-setuid-sandbox',
    '--headless=new',
    '--disable-gpu',
    '--disable-dev-shm-usage',
    '--disable-software-rasterizer',
    '--remote-debugging-port=9222',
  ],
  port: 3000,
  workers: parseInt(process.env.PRERENDER_NUM_WORKERS || '2', 10),
  iterations: parseInt(process.env.PRERENDER_NUM_ITERATIONS || '40', 10),
  pageLoadTimeout: 20000,
  waitAfterLastRequest: 500,
});

server.start();
