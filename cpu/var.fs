\ VAR instructions

\ Stack effect: ( argN ... arg2 arg1 n-args -- )

32 array VAROPS

\ call_vs routine args... -> (result)
:noname swap pa swap   true zcall ; 0 VAROPS !

\ storew array word-index value
:noname ( value index array 3 -- ) drop ba swap 2 * + w! ; 1 VAROPS !

\ storeb array byte-index value
:noname ( value index array 3 -- ) drop ba          + b! ; 2 VAROPS !

\ put_prop obj prop value
:noname ( value prop obj 3 -- )
  drop prop-find ( value ra-prop )
  dup prop-size  ( value ra-prop size )
  1 = IF prop-data b! ELSE prop-data w! THEN
; 3 VAROPS !



\ Converts a character to lowercase.
: lower-case ( c -- c ) dup 65 91 within IF 32 + THEN ;

\ Lower-cases a whole string.
: lower-case-str ( c-addr u -- )
  0 ?DO
    dup i +   dup c@ lower-case swap c!
  LOOP
  drop
;


create read-buffer 256 allot align
256 allot align

: read-debug ( parse -- )
  dup 1+ b@  ( parse words )
  0 DO
    dup 2 + i 2 lshift + ( parse addr )
    ." Parsed: "
    dup w@ hex.
    dup 2 + b@ hex.
    3 + b@ hex.
    CR
  LOOP
  drop
;


: zaccept ( text -- len )
  read-buffer over b@ ( text buf maxlen )
  accept              ( text len )
  dup >r
  0 ?DO read-buffer i + c@   lower-case   over i + 1+ b! LOOP
  drop r> ( len )
;

: v3read ( parse text 2 -- )
  drop ( parse text )
  print-v3-status

  \ Read into read-buffer, then copy into the Z-machine.
  dup zaccept ( parse text len )
  cr

  >r 1+ r>         \ Bump text to the first character.
  2dup + 0 swap b! \ Write the 0-terminator.

  2 pick IF parse-line ELSE drop 2drop THEN
;

: v4read ( routine time parse text n-args -- )
  >r \ Set aside the number of args for later
  2 v3read ( routine time ) \ maybe
  r> 2 -
  discard-args
;


\ TODO Needs support for other terminating characters.
\ TODO Support partly-filled input buffer.
: v5read ( routine time parse text n-args -- )
  >r \ Set aside the arg count.
  \ Slightly hacky, since I'm asking Forth to write into the Z-machine's buffer.
  ba dup 2 + ram + over b@ ( parse text c-addr maxlen )
  2dup accept           ( parse text c-addr maxlen len )
  nip 2dup lower-case-str ( parse text c-addr len )
  nip                   ( parse text len )
  2dup swap 1+ b!       ( parse text len ) \ Write read legth into text buffer

  \ Check that we were provided a parse buffer.
  r@ ( parse text len n-args )
  1 = IF ( text len   R: n )
    r> drop 2drop
    ( -- parse wasn't actually present )
    13 zstore EXIT
  THEN

  \ Only parse if the parse buffer is nonzero.
  >r over IF 2 + r> parse-line ELSE r> drop 2drop THEN
  r> 2 - ( n-args' )
  discard-args

  \ Always returning 13 for now.
  13 zstore
;

\ v1-3: sread text parse
\ v4:   sread text parse time routine
\ v5:   aread text parse time routine -> (result)
:noname
  version
  dup 4  = IF drop v4read EXIT THEN
      3 <= IF v3read ELSE v5read THEN
; 4 VAROPS !


\ print_char output-char-code
:noname ( char 1 -- ) drop emit ; 5 VAROPS !

\ print_num value
:noname ( value 1 -- ) drop signed 0 .r ; 6 VAROPS !

\ random range -> (result)
:noname ( range 1 -- )
  drop signed
  dup 0= IF
    drop true-seed 0
  ELSE
    dup 0< IF
      negate seed 0
    ELSE
      random
    THEN
  THEN
  zstore
; 7 VAROPS !


\ push value
:noname ( value 1 -- ) drop push ; 8 VAROPS !

\ pull (var)
:noname ( var 1 -- )
  drop pop swap ( val var )
  dup 0= IF drop sp @ ! ELSE var! THEN
; 9 VAROPS !

\ split_window
:noname ( lines 1 -- )
  drop
  \ In version 3, we need to leave extra room for the status line.
  version 3 <= IF 1+ THEN
  dup resize-upper-window
  version 3 = IF
    \ Erase each line of the upper window.
    cursor-save
    1+ 1 DO
      i 0 cursor-move
      erase-line
    LOOP
    cursor-restore
  ELSE
    drop
  THEN
; 10 VAROPS !

\ set_window
:noname ( win 1 -- )
  drop
  \ Window 0 is the bottom, scrolling window. Window 1 is the top window.
  IF \ switch to top window
    term-window @ 0= IF \ Currently in the bottom window.
      \ Save the cursor.
      cursor-position cursor-upper 2!
      1 term-window !
    THEN
    \ Whether we moved or not, reset the cursor to 2,1 if this is version 3,
    \ 1,1 otherwise.
    1   version 3 = IF 1+ THEN   0 cursor-move
  ELSE \ switch to bottom window
    term-window @ IF \ Currently in the top window.
      cursor-position cursor-upper 2!
      cursor-lower 2@ cursor-move
      0 term-window !
    THEN \ Do nothing if we're already in the bottom window.
  THEN
; 11 VAROPS !

\ call_vs2 routine args... (up to 7) -> (result)
:noname ( args... routine n -- ) swap pa swap   true zcall ; 12 VAROPS !

\ erase_window
\ Erase window 0 or 1.
\ -1 means unsplit and clear all.
\ -2 means clear all but don't unsplit.
:noname ( win 1 -- )
  drop CASE
    0 OF
      cursor-save
      term-rows @   upper-window-size @ 1+ DO
        i 0 cursor-move
        erase-line
      LOOP
      cursor-restore
    ENDOF

    1 OF
      cursor-save
      upper-window-size @ 1 DO
        i 0 cursor-move
        erase-line
      LOOP
      cursor-restore
    ENDOF

    -1 OF
      \ Unsplit and then clear everything.
      1   version 3 = and   resize-upper-window
      erase-screen
    ENDOF

    -2 OF
      \ Clear everything but don't unsplit.
      erase-screen
    ENDOF
  ENDCASE
; 13 VAROPS !

\ erase_line
:noname ( value 1 -- )
  \ Do nothing if value is not 1.
  \ Erases from the cursor through the end of the line.
  drop 1 = IF erase-to-eol THEN
; 14 VAROPS !

\ set_cursor
:noname ( col lin 2 -- )
  drop
  swap cursor-move
; 15 VAROPS !

\ get_cursor
\ Puts the row into word 0, line into word 1.
:noname ( array 1 -- )
  drop cursor-position ( array row col )
  >R over w!
  2 + R> ( array' col )
  swap w!
; 16 VAROPS !

\ set_text_style style
:noname ( style 1 -- )
  drop
  dup 0= IF drop term-roman EXIT THEN
  dup 1 and IF term-reverse THEN
  dup 2 and IF term-bold THEN
  dup 4 and IF term-italic THEN
  drop
  \ No need to support fixed-pitch; it's always on.
; 17 VAROPS !

:noname ( flag 1 -- ) 2drop ." [Unimplemented: buffer_mode]" cr ; 18 VAROPS !

\ Expects 2 arguments.
\ TODO Implement more output streams.
:noname ( table num 1 -- ) discard-args ; 19 VAROPS !

:noname ( num 1 -- ) 2drop ." [Unimplemented: input_stream]" cr ; 20 VAROPS !
:noname ( ... n -- )
  discard-args ." [Unimplemented: sound_effect]" cr ; 21 VAROPS !

\ read_char 1 time routine -> (result)
\ TODO Handle time and routine, where provided.
:noname ( ... n -- ) discard-args   key   dup emit   zstore ; 22 VAROPS !

\ scan_table x table len opt_form -> (result) (?label)
:noname ( opt_form len table x n -- )
  dup 3 = IF \ No form, add it.
    >r >r >r >r $82 r> r> r> r>
  THEN
  drop
  ( form len table x )
  >r >r over 127 and * ( form len-bytes   R: x table )
  \ Bury a 0 as the default answer.
  0 -rot               ( 0 form len-bytes   R: x table )
  r>                   ( 0 form len-bytes table   R: x )
  tuck + swap ( 0 form end start )
  r> -rot     ( 0 form x end start )
  DO ( 0 form x )
    over 128 and IF i w@ ELSE i b@ THEN ( 0 form x y )
    over = IF 2drop drop   i 0 0   ( ra 0 0 ) UNLOOP LEAVE THEN
    ( 0 form x )
  over 127 and +LOOP
  2drop
  dup zstore \ Store the result.
  zbranch ( )
; 23 VAROPS !


\ not value -> (result)
:noname ( value 1 -- ) drop invert $ffff and zstore ; 24 VAROPS !

\ call_vn routine args...
:noname ( args... routine n -- ) swap pa swap   false zcall ; 25 VAROPS !
\ call_vn2 routine args...
:noname ( args... routine n -- ) swap pa swap   false zcall ; 26 VAROPS !


\ TODO Implement tokenize
\ tokenize text parse dictionary flag
:noname discard-args ." [Unimplemented: tokenize]" cr ; 27 VAROPS !
\ TODO Implement encode_text
\ encode_text zscii length from coded-text
:noname discard-args ." [Unimplemented: encode_text]" cr ; 28 VAROPS !


\ copy_table first second size
:noname ( size second first 3 -- )
  drop
  \ If second is 0, write 0s to first.
  over 0= IF ( size second first ) nip swap ba ram + erase EXIT THEN
  \ Convert both addresses to absolute Forth addresses.
  ba ram + swap ba ram +
  rot ( first second size )
  dup 0< IF negate cmove ELSE move THEN
; 29 VAROPS !


\ print_table zscii-text width height skip
\ TODO Implement print_table
:noname discard-args ." [Unimplemented: print_table]" cr ; 30 VAROPS !

\ check_arg_count argument-number
:noname ( arg-number 1 -- )
  \ NB: It counts from 1, so we use <= here.
  drop arg-count @ <= zbranch
; 31 VAROPS !

\ Special case VAR format je.
\ Branches if a is equal to any of the later values.
: var_je ( d c b a n -- )
  dup 1 = IF 2drop false zbranch EXIT THEN
  1- -rot ( d c n b a )
  2dup = IF drop swap discard-args   true zbranch EXIT THEN
  nip swap ( d c a n-1 )
  recurse
;

