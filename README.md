<h1>Flatten Map Plugin</h1>
<p>Loops though each of the entitys on the server and turns them in to prefabs.<br />Then saves the map in the servers rust root folder.</p>
<h3><br />Usage:</h3>
<p><strong><span style="color: #ff0000;">(RUN ON A SEPERATE SERVER NOT YOUR MAIN)<br /><br /></span></strong>Copy the map and .sav files from your main server to a seperate one.<br />Install plugin as usual.<br />Start the server up and run command from either chat or console.</p>
<h3><br />Command:</h3>
<p><br /><strong>Console:</strong> flattenmap "newmapname"<br /><strong>Chat:</strong> /flatten "newmapname"<br /><br /><em>Options you can provide after the new map name in order they can be provided.</em></p>
<p style="padding-left: 40px;"><br />true/false wipe maps prefabs before flatten (useful if you only want player bases)<br />true/false apply building grade to players bases. (will be twig if false)<br />true/false filter out base players / npcs from maps. (if false piles of loot from players/npcs will be where players/npcs would of been)<br />true/false filter out entitys at 0,0,0 (will be server scripts and junk collection)<br />true/false filter out code/key locks<br />true/false replace players/npcs with place holders/spawners<br />true/false try process IO data.</p>
<p><br /><strong>Example:</strong> <br />(default mode)<br />/flatten "mapdump"<br />(dump map using options)<br />/flatten "mapdump" false true true true true false true</p>
