\ Central structures and components of the Z-machine.

\ Maximum size of the memory is 512KB. Reserve that now.
524288 carray ram

\ Stores the real Z-machine address for PC.
VARIABLE pc


\ Reads the byte under PC. DOES NOT advance it.
: pc@ ( -- u ) pc @ b@ ;

\ Adjusts PC by the given amount.
: pc+ ( n -- ) pc +! ;

\ Bumps PC by one.
: pc++ ( -- ) 1 pc+ ;

\ Reads the byte at PC and advances it.
: pc@+ ( -- u ) pc@ pc++ ;

\ Reads a word at PC and advances it.
: pc@w+ ( -- u ) pc@ 8 lshift   pc@   or ;


\ The Z-machine stack. Not directly accessible, so I can format it however.
\ It's going to be used to store locals and routine state as well as things
\ pushed by the game.
\ Stack design is full-descending, ie. decrement-before-store
1024 cells allot
here @ CONSTANT stack-top
VARIABLE sp
stack-top sp !

VARIABLE fp

\ Routines use a frame pointer in C fashion.
\ See cpu/util.fs for a spec on that.
: push ( x -- ) sp @   1 cells -   dup sp ! ! ;
: peek ( -- x ) sp @ @ ;
: pop  ( -- x ) sp @   dup @   swap cell+ sp ! ;

