\ Opcode implementations and instruction decoding.

: read-arg ( type -- val #t | #f )
  3 and
  CASE
  0 OF pc@w+ true ENDOF
  1 OF pc@+  true ENDOF
  2 OF pc@+ var@ true ENDOF
  3 OF false ENDOF
  ENDCASE
;



\ Short form
\ bits 4 and 5 are a type. If it's $$11, 0OP. Otherwise 1OP.
: short-form ( opcode )
  dup 15 and swap 4 rshift read-arg ( opcode [val #t | #f] )
  IF swap 1OPS @ execute ELSE 0OPS @ execute THEN
;

\ Long form
\ Bit 6 gives the first type, bit 5 the second.
\ A 0 means small constant, 1 means a variable.
\ Adding 1 produces 1 and 2, which are the types.
: long-form ( opcode )
  dup 31 and swap ( op-num code )
  dup  6 rshift 1 and 1+ read-arg drop ( op-num code arg1 )
  swap 5 rshift 1 and 1+ read-arg drop ( op-num arg1 arg2 )
  swap ( op-num arg2 arg1 )
  rot 2OPS @ execute
;


\ Variable form

8 array var-args
variable var-count

: add-var-arg ( value -- )
  var-count @ ( value index )
  var-args  ! ( )
  1 var-count +!
;

: pop-var-args ( -- args... n-args )
  var-count @ ( count )
  BEGIN dup 0> WHILE ( ... count )
    1- ( ... index/count' )
    dup var-args @ ( ... count' value )
    swap  ( ... value count' )
  REPEAT
  drop ( args... )
  var-count @ ( args... n-args )
;

\ Internal helper for read-var-args.
: (read-var-args)
  dup 6 rshift read-arg IF add-var-arg THEN
  dup 4 rshift read-arg IF add-var-arg THEN
  dup 2 rshift read-arg IF add-var-arg THEN
               read-arg IF add-var-arg THEN
;

: read-var-args ( n-bytes -- args... n-args )
  >R
  0 var-count !
  pc@+
  R@ 1 > IF pc@+ swap THEN
  (read-var-args)
  R@ 1 > IF (read-var-args) THEN
  pop-var-args
  R> drop
;

: variable-form ( opcode -- )
  \ Special case for variable form @je
  dup $c1 = IF drop 1 read-var-args   var_je EXIT THEN
  dup 31 and swap 32 and ( opcode-number var? )
  IF   ( op-num ) \ VAR count
    \ Special case for the two double-call operations.
    dup >r CASE   ( R: op-num )
    12 OF 2 read-var-args ENDOF
    26 OF 2 read-var-args ENDOF
    drop 1 read-var-args 0 ( args... n-args dummy )
    ENDCASE ( args... n-args   R: op-num )
    r>
    VAROPS @ execute
  ELSE ( op-num ) \ 2OP count
    >r 1 read-var-args drop r> 2OPS @ execute
  THEN
;


\ Extended form is almost the same as (the simple case of) variable form.
: extended-form ( opcode=$be -- )
  drop pc@+ >r  ( R: ext-opcode )
  1 read-var-args ( args... n-args   R: ext-opcode )
  r> EXTOPS @ execute
;

\ Runs a single opcode.
: execute-op ( -- )
  pc@+
  dup $be =   version 5 >= and IF extended-form EXIT THEN
  dup 6 rshift 3 and ( opcode top-two-bits )
  CASE
  3 OF variable-form ENDOF
  2 OF short-form    ENDOF
  drop long-form 0
  ENDCASE
  depth 0> IF
    ." Opcode at " pc @ hex. ."  left junk on the stack: " .s
    ABORT
  THEN
;

