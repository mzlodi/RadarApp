var url = require('url');
var http = require('http');

var g_http_server = null;
var g_current_targets_json = [];
var g_current_targets_full = [];
var g_pattern = 'random';
var g_max_targets = 1;
var g_target_lifetime = 30;
var g_target_delay = 10;

var g_prev_creation_time = 0;
var g_next_id = 0;

function start_http(port) {
  g_http_server = http.createServer(handle_web_request);
  g_http_server.listen(port ? port : 80); 
}

function handle_web_request(request, response) {
  var qs = url.parse(request.url, true);
  var headers = request.headers;

  if (qs.pathname != '/scan_radars') {
    response.writeHead(404, {"Content-Type": "text/plain"});
    response.write("404 Not found");
    response.end();
    return;
  }

  var response_object = { targets: g_current_targets_json, timestamp: Math.round(+new Date()/1000) };
  response.writeHead(200, {
    "Content-Type": "application/json", 
    "Access-Control-Allow-Origin" : "*", 
    "Access-Control-Allow-Headers": "X-Requested-With, Content-Type"
  });
  response.write(JSON.stringify(response_object));  
  response.end();
}

function start_target_simulator() {
  setInterval(update_targets, 100);
}

function update_targets() {
  create_new_targets();
  delete_old_targets();
  move_existing_targets();
  update_json();
}

function create_new_targets() {
  if ((+ new Date())/1000 - g_prev_creation_time < g_target_delay)
    return;

  if ((Math.random() * 5 * g_target_delay > 1) && (g_prev_creation_time != 0))
    return;

  if (g_current_targets_full.length >= g_max_targets)
    return;

  var y = 25 + Math.random() * 100;
  var x = Math.tan(20 * Math.PI / 180) * y;
  if (Math.random() < 0.5)
    x = -x;

  g_current_targets_full.push({
    id: g_next_id ++,
    strength: Math.random() * 1000,
    create_time: (+ new Date())/1000,
    angle: Math.random() * 360,
    x: x,
    y: y,
    speed: Math.random() * 4 + 4
  });

  g_prev_creation_time = (+ new Date())/1000;

  console.log("New target: id=" + (g_next_id - 1));
}

function delete_old_targets() {
  var to_delete = [];
  for (var i = 0; i < g_current_targets_full.length; i++) {
    if ((+ new Date())/1000 - g_current_targets_full[i].create_time > g_target_lifetime)
      to_delete.push(i);
    else if (((+ new Date())/1000 - g_current_targets_full[i].create_time > 5) && (Math.random() * 10 * g_target_lifetime < 1))
      to_delete.push(i);
    else if (g_current_targets_full[i].y < 0)
      to_delete.push(i);
    else if (g_current_targets_full[i].y > 120)
      to_delete.push(i);
  }

  for (var i = to_delete.length - 1; i >= 0; i--) {
    console.log("Delete target: id=" + (g_current_targets_full[to_delete[i]].id));
    g_current_targets_full.splice(to_delete[i], 1);
  }
}

function move_existing_targets() {
  for (var i = 0; i < g_current_targets_full.length; i++) {
    var delta_t = 0.1;
    var offset = g_current_targets_full[i].speed * delta_t / 3.6;
    g_current_targets_full[i].x += offset * Math.cos(g_current_targets_full[i].angle * Math.PI / 180);
    g_current_targets_full[i].y += offset * Math.sin(g_current_targets_full[i].angle * Math.PI / 180);
    g_current_targets_full[i].relative_speed = -g_current_targets_full[i].speed * Math.sin(g_current_targets_full[i].angle * Math.PI / 180);
    if (g_pattern == 'random') {
      g_current_targets_full[i].angle += Math.random() * 9 - 4.5;
      g_current_targets_full[i].speed += Math.random() * 1 - 0.5;
    }
  }
}

function update_json() {
  g_current_targets_json = [];
  for (var i = 0; i < g_current_targets_full.length; i++) {
    g_current_targets_json.push({
      id: g_current_targets_full[i].id,
      distance: Math.sqrt(g_current_targets_full[i].x * g_current_targets_full[i].x + g_current_targets_full[i].y * g_current_targets_full[i].y),
      angle: Math.atan2(g_current_targets_full[i].x, g_current_targets_full[i].y) * 180 / Math.PI,
      speed: g_current_targets_full[i].relative_speed,
      strength: g_current_targets_full[i].strength
    });
  }
}

function print_usage() {
  console.log("Geolux RSS-2-300 CS radar simulator");
  console.log("Usage: ");
  console.log("node simulator.js http_port pattern max_targets target_lifetime target_delay");
  console.log("   http_port         - the port on which HTTP server will listen for polling requests");
  console.log("   pattern           - defines target motion, can be any of the values:");
  console.log("                       line   -> line movement");
  console.log("                       random -> random movement");
  console.log("   max_targets       - the maximum possible number of simultaneous targets");
  console.log("   target_lifetime   - the maximum target lifetime in seconds");
  console.log("   target_delay      - minimum delay between creating two new targets in seconds");
}

if (process.argv.length != 7) {
  print_usage();
  return;
}

g_pattern = process.argv[3];
g_max_targets = parseInt(process.argv[4]);
g_target_lifetime = parseInt(process.argv[5]);
g_target_delay = parseInt(process.argv[6]);

if ((g_pattern != 'line') && (g_pattern != 'random')) {
  print_usage();
  return;
}

start_http(parseInt(process.argv[2]));
start_target_simulator();
