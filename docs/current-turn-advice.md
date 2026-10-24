# Collapsed current-turn advice · Windows 0.2.19

The collapsed panel lists all remaining steps of the current turn, numbered
from one and including the first EndTurn. Later turns remain available through
the existing full-plan button. If the available forecast ends before EndTurn,
the collapsed panel displays its available steps without inventing an EndTurn.
The existing scroll area handles turns whose steps exceed the compact panel.

This changes text formatting only. Expanding/collapsing still preserves the
same advice object and starts no capture or search. Input and continuation
behavior are unchanged. The existing isolated UI acceptance now checks the
last current-turn step, exclusion of later steps, unchanged advice identity,
and unchanged capture/search counts.
