\ Copyright (c) 2019, Travis Bemann
\ All rights reserved.
\ 
\ Redistribution and use in source and binary forms, with or without
\ modification, are permitted provided that the following conditions are met:
\ 
\ 1. Redistributions of source code must retain the above copyright notice,
\    this list of conditions and the following disclaimer.
\ 
\ 2. Redistributions in binary form must reproduce the above copyright notice,
\    this list of conditions and the following disclaimer in the documentation
\    and/or other materials provided with the distribution.
\ 
\ 3. Neither the name of the copyright holder nor the names of its
\    contributors may be used to endorse or promote products derived from
\    this software without specific prior written permission.
\ 
\ THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
\ AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
\ IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
\ ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
\ LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
\ CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
\ SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
\ INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
\ CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
\ ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
\ POSSIBILITY OF SUCH DAMAGE.

\ TRUE constant macro
: +true -1 vm lit not-vm ;

\ FALSE constant macro
: +false 0 vm lit not-vm ;

vm

\ TRUE constant
define-word true +true end-word

\ FALSE constant
define-word false +false end-word

\ Cell size in bytes constant
define-word cell-size ( -- u ) target-cell-size lit end-word

\ Advance a pointer by one cell
define-word cell+ ( u -- u ) target-cell-size lit + end-word

\ Multiply a value by the size of a cell
define-word cells ( u -- u ) target-cell-size lit * end-word

\ Small token size in bytes constant
define-word half-token-size ( -- u ) target-half-token-size lit end-word

\ Full token size in bytes constant
define-word full-token-size ( -- u ) target-full-token-size lit end-word

\ Get whether tokens use a token size bit
define-word token-flag-bit ( -- flag )
  half-token-size full-token-size <>
end-word

\ The user space pointer
define-word-created up
0 set-cell-data

\ The HERE pointer is stored here
define-word-created user-space-current
0 set-cell-data

\ Get the HERE pointer
define-word here ( -- addr ) user-space-current @ end-word

\ Set the HERE pointer
define-word here! ( addr -- ) user-space-current ! end-word

\ Advance or, with negative values, retract the HERE pointer by a number of
\ bytes
define-word allot ( u -- ) here + here! end-word

\ The built-in user variable count
not-vm variable build-#user vm

\ The built-in user variable macro
: define-word-user ( "name" -- )
  define-word
  not-vm build-#user @ dup 1 + build-#user ! vm lit cells up @ +
  end-word ;

\ Negate a value
define-word negate ( n -- n ) 1 lit - not end-word

\ Duplicate the value under the top of the stack
\ DEFINE-WORD OVER ( x1 x2 -- x1 x2 x1 ) 1 LIT PICK END-WORD

\ Drop the value under the top of the stack
define-word nip ( x1 x2 -- x2 ) swap drop end-word

\ Insert the value at the top of the stack under the value under the top of
\ the stack
define-word tuck ( x1 x2 -- x2 x1 x2 ) swap over end-word

\ Duplicate the two values on the top of the stack, preserving their order
define-word 2dup ( x1 x2 -- x1 x2 x1 x2 ) over over end-word

\ Drop the two values on the top of the stack
define-word 2drop ( x1 x2 -- ) drop drop end-word

\ Drop a value from the top of the return stack
define-word rdrop ( R: x1 x2 -- x2 ) r> r> r> drop >r >r end-word

\ Get whether one value is smaller than (signed) or equal to another value
define-word <= ( n1 n2 -- flag ) 2dup = rot rot < or end-word

\ Get whether one value is greater than (signed) or equal to another value
define-word >= ( n1 n2 -- flag ) 2dup = rot rot > or end-word

\ Fetch a value at an address, add a value to it, and store it to the same
\ address
define-word +! ( n addr -- ) dup @ rot + swap ! end-word

\ Duplicate a value on the top of the stack if it is non-zero
define-word ?dup ( 0 | x -- 0 | x x ) dup 0 lit <> +if dup +then end-word

\ Store a cell-sized value at the HERE pointer and advance the HERE pointer by
\ one cell
define-word , ( x -- ) here ! cell-size allot end-word

\ Store a byte-sized value at the HERE pointer and advance the HERE pointer by
\ one byte
define-word c, ( c -- ) here c! 1 lit allot end-word

\ Store a 16-bit value at the HERE pointer and advance the HERE pointer by two
\ bytes
define-word h, ( c -- ) here h! 2 lit allot end-word

\ Store a 32-bit value at the HERE pointer and advance the HERE pointer by four
\ bytes
define-word w, ( c -- ) here w! 4 lit allot end-word

\ Compile a token at the HERE pointer and advance the HERE pointer by the token
\ size
define-word compile, ( token -- )
  not-vm target-token @ token-8-16 = vm lit +if
    dup $80 lit u< +if
      c,
    +else
      dup $7f lit and $80 lit or c, 7 lit rshift 1 lit - $ff lit and c,
    +then
  +else
    not-vm target-token @ token-16 = vm lit +if
      $ffff lit and h,
    +else
      not-vm target-token @ token-16-32 = vm lit +if
        dup $8000 lit u< +if
          h,
        +else
          dup $7fff lit and $8000 lit or h, 15 lit rshift 1 lit -
	  $ffff lit and h,
	+then
      +else ( TARGET-TOKEN @ TOKEN-32 NOT-VM = VM )
        $ffffffff lit and w,
      +then
    +then
  +then
end-word

\ Standard input file descriptor constant
define-word stdin ( -- fd ) 0 lit end-word

\ Standard output file descriptor constant
define-word stdout ( -- fd ) 1 lit end-word

\ Standard error file descriptor constant
define-word stderr ( -- fd ) 2 lit end-word

\ Input file descriptor user variable
define-word-user input-fd

\ Output file descriptor user variable
define-word-user output-fd

\ Error file descriptor user variable
define-word-user error-fd

\ TYPE hook
define-word-created 'type
0 set-cell-data

\ KEY? hook
define-word-created 'key?
0 set-cell-data

\ KEY hook
define-word-created 'key
0 set-cell-data

\ ACCEPT hook
define-word-created 'accept
0 set-cell-data

\ BYE hook
define-word-created 'bye
0 set-cell-data

\ TYPE wrapper
define-word type ( c-addr bytes -- ) 'type @ execute end-word

\ KEY? wrapper
define-word key? ( -- flag ) 'key? @ execute end-word

\ KEY wrapper
define-word key ( -- c ) 'key @ execute end-word

\ ACCEPT wrapper
define-word accept ( c-addr bytes1 -- bytes2 ) 'accept @ execute end-word

\ BYE wrapper
define-word bye ( -- ) 'bye @ execute end-word

\ Space constant
define-word bl $20 lit end-word

\ Newline (LF) constant
define-word newline $0a lit end-word

\ Tab constant
define-word tab $09 lit end-word

\ Output a single character on standard output
define-word emit ( c -- )
  here c! here 1 lit allot 1 lit type -1 lit allot end-word

\ Output a single space on standard output
define-word space ( -- ) bl emit end-word

\ Output a single newline on standard output
define-word cr ( -- ) newline emit end-word

\ Error writing to standard output
define-word x-unable-to-write-stdout ( -- ) bye end-word

\ Error reading from standard input
define-word x-unable-to-read-stdin space ( -- )
  space s" unable to read from standard input" +data type cr
end-word

\ Standard input is supposed to be blocking
define-word x-is-supposed-to-block ( -- )
  space s" standard input is supposed to block" +data type cr
end-word

\ The handler user variable
define-word-user handler

\ Global exception handler
define-word-created 'global-handler
0 set-cell-data

\ Global bye handler
define-word-created 'global-bye-handler
0 set-cell-data

\ Execute an xt, returning either zero if no exception takes place, or an
\ exception value if an exception has taken place
define-word try ( xt -- exception | 0 )
  sp@ >r handler @ >r rp@ handler ! execute r> handler ! r> drop 0 lit
end-word

\ If the passed-in value is non-zero, raise an exception; note that this value
\ should be an xt, since uncaught exception values are normally executed as an
\ xt
define-word ?raise ( xt | 0 -- xt | 0 )
  dup +if 'global-handler @ ?dup +if execute +then +then
  ?dup +if handler @ rp! r> handler ! r> swap >r sp! drop r> +then
end-word

\ IF conditional
define-word-immediate-compile-only if ( f -- ) ( Compile-time: -- fore-ref )
  &0branch compile, here 0 lit ,
end-word

\ ELSE conditional
define-word-immediate-compile-only else ( -- )
  ( Compile-time: fore-ref -- fore-ref )
  &branch compile, here 0 lit , here rot !
end-word

\ THEN conditional
define-word-immediate-compile-only then ( -- ) ( Compile-time: fore-ref -- )
  here swap !
end-word

\ Start of BEGIN/AGAIN, BEGIN/UNTIL, and BEGIN/WHILE/REPEAT loops
define-word-immediate-compile-only begin ( -- ) ( Compile-time: -- back-ref )
  here
end-word

\ Jump back unconditionally to the matching BEGIN
define-word-immediate-compile-only again ( -- ) ( Compile-time: back-ref -- )
  &branch compile, ,
end-word

\ Jump back to the matching BEGIN until a value taken off the top of the stack
\ is non-zero
define-word-immediate-compile-only until ( -- ) ( Compile-time: back-ref -- )
  &0branch compile, ,
end-word

\ Jump forward to the matching REPEAT if the value taken off the top of the
\ stack is zero
define-word-immediate-compile-only while ( -- ) ( Compile_time: -- fore-ref )
  &0branch compile, here 0 lit ,
end-word

\ Jump back to the matching BEGIN, and be jumped after by the matching WHILE
define-word-immediate-compile-only repeat ( -- )
  ( Compile-time: back-ref fore-ref -- )
  swap &branch compile, , here swap !
end-word

\ Unimplemented service exception
define-word x-unimplemented-sys ( -- )
  space s" unimplemented service" +data type cr
end-word

\ New name for the original SYS opcode because VM assembly words have no
\ "hidden" flag
define-word old-sys ( sys -- ) sys end-word

\ A wrapper for the SYS opcode to handle unimplemented services, raising an
\ exception if they occur
define-word sys ( sys -- )
  old-sys 0 lit = +if &x-unimplemented-sys ?raise +then
end-word

\ Look up a service by name and return either the service ID, if the service
\ exists, or zero, if it does not exist
define-word sys-lookup ( addr bytes -- service|0 ) 1 lit sys end-word

\ Read service ID
define-word-created sys-read
0 set-cell-data

\ Write service ID
define-word-created sys-write
0 set-cell-data

\ Get nonblocking service ID
define-word-created sys-get-nonblocking
0 set-cell-data

\ Set nonblocking service ID
define-word-created sys-set-nonblocking
0 set-cell-data

\ Bye service ID
define-word-created sys-bye
0 set-cell-data

\ Get trace service ID
define-word-created sys-get-trace
0 set-cell-data

\ Set trace service ID
define-word-created sys-set-trace
0 set-cell-data

\ Set name table service ID
define-word-created sys-set-name-table
0 set-cell-data

\ Get interrupt handler service ID
define-word-created sys-get-int-handler
0 set-cell-data

\ Set interrupt handler service ID
define-word-created sys-set-int-handler
0 set-cell-data

\ Get interrupt mask service ID
define-word-created sys-get-int-mask
0 set-cell-data

\ Set interrupt mask service ID
define-word-created sys-set-int-mask
0 set-cell-data

\ Adjust interrupt mask service ID
define-word-created sys-adjust-int-mask
0 set-cell-data

\ Get interrupt handler mask service ID
define-word-created sys-get-int-handler-mask
0 set-cell-data

\ Set interrupt handler mask service ID
define-word-created sys-set-int-handler-mask
0 set-cell-data

\ Get alarm service ID
define-word-created sys-get-alarm
0 set-cell-data

\ Set alarm service ID
define-word-created sys-set-alarm
0 set-cell-data

\ Look up a number of services
define-word lookup-sys ( -- )
  s" READ" +data sys-lookup sys-read !
  s" WRITE" +data sys-lookup sys-write !
  s" GET-NONBLOCKING" +data sys-lookup sys-get-nonblocking !
  s" SET-NONBLOCKING" +data sys-lookup sys-set-nonblocking !
  s" BYE" +data sys-lookup sys-bye !
  s" GET-TRACE" +data sys-lookup sys-get-trace !
  s" SET-TRACE" +data sys-lookup sys-set-trace !
  s" SET-NAME-TABLE" +data sys-lookup sys-set-name-table !
  s" SET-NAME-TABLE" +data sys-lookup sys-set-name-table !
  s" GET-INT-HANDLER" +data sys-lookup sys-get-int-handler !
  s" SET-INT-HANDLER" +data sys-lookup sys-set-int-handler !
  s" GET-INT-MASK" +data sys-lookup sys-get-int-mask !
  s" SET-INT-MASK" +data sys-lookup sys-set-int-mask !
  s" ADJUST-INT-MASK" +data sys-lookup sys-adjust-int-mask !
  s" GET-INT-HANDLER-MASK" +data sys-lookup sys-get-int-handler-mask !
  s" SET-INT-HANDLER-MASK" +data sys-lookup sys-set-int-handler-mask !
  s" GET-ALARM" +data sys-lookup sys-get-alarm !
  s" SET-ALARM" +data sys-lookup sys-set-alarm !
end-word

\ Read a file descriptor (a return value of -1 means success, a return value of
\ 0 means error, and a return value of 1 means that accessing would block were
\ it not for non-blocking).
define-word read ( c-addr bytes fd -- bytes-read -1|0|1 )
  sys-read @ sys
end-word

\ Write a file descriptor (a return value of -1 means success, a return value of
\ 0 means error, and a return value of 1 means that accessing would block were
\ it not for non-blocking).
define-word write ( c-addr bytes fd -- bytes-written -1|0|1 )
  sys-write @ sys
end-word

\ Get whether a file descriptor is non-blocking (a return value of -1 means
\ success and a return value 0f 0 means error).
define-word get-nonblocking ( fd -- non-blocking -1|0 )
  sys-get-nonblocking @ sys
end-word

\ Get whether a file descriptor is blocking (a return value of -1 means success
\ and a return value of 0 means error).
define-word set-nonblocking ( non-blocking fd -- -1|0 )
  sys-set-nonblocking @ sys
end-word

\ Get the trace flag
define-word get-trace ( -- trace-flag ) sys-get-trace @ sys end-word

\ Set the trace flag
define-word set-trace ( trace-flag -- ) sys-set-trace @ sys end-word

\ Set the name table
define-word set-name-table ( addr -- ) sys-set-name-table @ sys end-word

\ Get an interrupt handler
define-word get-int-handler ( index -- xt ) sys-get-int-handler @ sys end-word

\ Set an interrupt handler
define-word set-int-handler ( xt index -- ) sys-set-int-handler @ sys end-word

\ Get an interrupt mask
define-word get-int-mask ( -- mask ) sys-get-int-mask @ sys end-word

\ Set an interrupt mask
define-word set-int-mask ( -- mask ) sys-set-int-mask @ sys end-word

\ Adjust an interrupt mask
define-word adjust-int-mask ( and or -- ) sys-adjust-int-mask @ sys end-word

\ Get an interrupt handler mask
define-word get-int-handler-mask ( index -- mask )
  sys-get-int-handler-mask @ sys
end-word

\ Set an interrupt handler mask
define-word set-int-handler-mask ( mask index -- )
  sys-set-int-handler-mask @ sys
end-word

\ Get an alarm
define-word get-alarm ( alarm -- int-s int-ns val-s val-ns flag )
  sys-get-alarm @ sys
end-word

\ Set an alarm
define-word set-alarm
  ( nint-s nint-ns nval-s nval-ns alarm -- oint-s oint-ns oval-s oval-ns flag )
  sys-set-alarm @ sys
end-word

\ Whether to treat IO as single-tasking.
define-word-created single-task-io
0 set-cell-data

\ The numeric base user variable
define-word-user base

\ The size of the buffer for numeric formatting
not-vm 65 constant max-format-digit-count vm

\ A buffer for numeric formatting for the main task
define-word-created main-format-digit-buffer
max-format-digit-count 0 set-fill-data

\ A user variable poiting to a format digit buffer
define-word-user format-digit-buffer

\ Convert a pointer into the numeric fomatting buffer into a pointer and a
\ length in bytes
define-word complete-format-digit-buffer ( c-addr -- c-addr u )
  format-digit-buffer @ max-format-digit-count lit + over -
end-word

\ Add a character to the numeric formatting buffer and return an updated
\ pointer
define-word add-char ( c c-addr -- c-addr ) 1 lit - tuck c! end-word

\ The inner loop for formatting decimal numbers
define-word (format-decimal) ( n -- c-addr )
  format-digit-buffer @ max-format-digit-count lit + +begin
    over 0 lit u>
  +while
    over 10 lit umod char 0 lit + swap add-char swap 10 lit u/ swap
  +repeat
  nip
end-word

\ Handle the outer parts of formatting decimal numbers
define-word format-decimal ( n -- c-addr u )
  dup 0 lit < +if
    negate (format-decimal) char - lit swap add-char
    complete-format-digit-buffer
  +else
    (format-decimal) complete-format-digit-buffer
  +then
end-word

\ Inner portion of formatting unsigned and non-decimal numbers
define-word (format-unsigned) ( n -- c-addr u )
  format-digit-buffer @ max-format-digit-count lit + +begin
    over 0 lit u>
  +while
    over base @ umod dup 10 lit u< +if
      char 0 lit +
    +else
      char A lit + 10 lit -
    +then
    swap add-char swap base @ u/ swap
  +repeat
  nip complete-format-digit-buffer
end-word

\ Format signed numbers and numbers of any base (note that non-decimal numbers
\ are treated as unsigned)
define-word format-number ( n -- c-addr u )
  dup 0 lit = +if
    drop char 0 lit format-digit-buffer @ c! format-digit-buffer @ 1 lit
  +else base @ 10 lit = +if
    format-decimal
  +else base @ dup 2 lit >= swap 36 lit <= and +if
    (format-unsigned)
  +else
    drop format-digit-buffer @ 0 lit
  +then +then +then
end-word

\ Format unsigned numbers
define-word format-unsigned ( n -- c-addr u )
  dup 0 lit = +if
    drop char 0 lit format-digit-buffer @ c! format-digit-buffer @ 1 lit
  +else base @ dup 2 lit >= swap 36 lit <= and +if
    (format-unsigned)
  +else
    drop format-digit-buffer @ 0 lit
  +then +then
end-word

\ Format count constant
define-word format-digit-count ( -- n )
  max-format-digit-count lit
end-word

\ Output a signed number on standard output with no following space
define-word (.) ( n -- ) format-number type end-word

\ Output an unsigned number on standard output with no following space
define-word (u.) ( u -- ) format-unsigned type end-word

\ Output a signed number on standard output with a following space
define-word . ( n -- ) (.) space end-word

\ Output an unsigned number on standard output with a following space
define-word u. ( u -- ) (u.) space end-word

\ Base address of the data stack (note the data stack grows downward)
define-word-created sbase
0 set-cell-data

\ Base address of the return stack (note the return stack grows downward)
define-word-created rbase
0 set-cell-data

\ Get the depth of the data stack
define-word depth ( -- u ) sbase @ sp@ - cell-size / 1 lit - end-word

\ Output the contents of the data stack, from deepest to topmost, on standard
\ output
define-word .s ( -- )
  depth 0 lit >= +if
    char [ lit emit space depth +begin dup 0 lit u> +while
      dup pick . 1 lit -
    +repeat
    drop
    char ] lit emit
  +else
    char [ lit emit char - lit emit char - lit emit char ] lit emit
  +then
  space
end-word

\ The size of the saved exception string
not-vm 256 constant max-saved-exception-len vm

\ The saved exception string buffer
define-word-created saved-exception-buffer
max-saved-exception-len 0 set-fill-data

\ The saved exception string length
define-word-created saved-exception-len
0 set-cell-data

\ Copy data from lowest to highest addresses
define-word cmove ( c-addr1 c-addr2 bytes -- )
  +begin dup 0 lit u> +while
    2 lit pick c@ 2 lit pick c! 1 lit - rot 1 lit + rot 1 lit + rot
  +repeat
  2drop drop
end-word

\ Copy data from highest to lowest addresses
define-word cmove> ( c-addr1 c-addr2 bytes -- )
  +begin dup 0 lit u> +while
    1 lit - 2 lit pick over + c@ 2 lit pick 2 lit pick + c!
  +repeat
  2drop drop
end-word

\ Copy data in such a manner that overlapping blocks of memory can be safely
\ copied to one another
define-word move ( c-addr1 c-addr2 bytes -- )
  2 lit pick 2 lit pick < +if cmove> +else cmove +then
end-word

\ Save a string for an exception
define-word save-exception ( c-addr bytes -- )
  dup max-saved-exception-len lit u> +if drop max-saved-exception-len lit +then
  dup saved-exception-len ! saved-exception-buffer swap cmove
end-word

\ Get a string saved for an exception
define-word saved-exception ( -- c-addr bytes )
  saved-exception-buffer saved-exception-len @
end-word

\ Advance a pointer and size for a buffer.
define-word advance-buffer ( c-addr bytes bytes-to-advance -- c-addr bytes )
  rot over + rot rot -
end-word

\ Single-tasking output to standard output implementation
define-word (type) ( c-addr bytes -- )
  output-fd @ get-nonblocking 0 lit = +if &x-unable-to-write-stdout ?raise +then
  +false output-fd @ set-nonblocking 0 lit = +if
    &x-unable-to-write-stdout ?raise
  +then
  rot rot +begin dup 0 lit > +while
    2dup output-fd @ write 0 lit = +if &x-unable-to-write-stdout ?raise +then
    advance-buffer
  +repeat
  2drop output-fd @ set-nonblocking 0 lit = +if
    &x-unable-to-write-stdout ?raise
  +then
end-word

\ Currently read character
define-word-user read-key

\ Whether a key has been read
define-word-user read-key?

\ Key already waiting to be read
define-word x-key-already-waiting ( -- )
  space s" KEY ALREADY WAITING" +data type cr
end-word

\ Set key read
define-word set-key ( c -- )
  read-key? @ +if &x-key-already-waiting ?raise +then
  +true read-key? ! read-key !
end-word

\ Clear key read
define-word clear-key ( -- ) 0 lit read-key ! +false read-key? ! end-word

\ Single-tasking implementation of testing whether a key is read.
define-word (key?) ( -- flag )
  read-key? @ +if
    +true
  +else
    input-fd @ get-nonblocking 0 lit = +if &x-unable-to-read-stdin ?raise +then
    +true input-fd @ set-nonblocking
    0 lit = +if &x-unable-to-read-stdin ?raise +then
    here 1 lit 1 lit allot input-fd @ read dup 0 lit = +if
      &x-unable-to-read-stdin ?raise
    +else -1 lit = +if
      -1 lit allot 1 lit = +if
        here c@ read-key ! +true read-key? ! +true
      +else
        &x-unable-to-read-stdin ?raise
      +then
    +else
      -1 lit allot drop +false
    +then +then
    swap input-fd @ set-nonblocking
    0 lit = +if &x-unable-to-read-stdin ?raise +then
  +then
end-word

\ Single-tasking implementation of reading a keypress from standard input.
define-word (key) ( -- c )
  read-key? @ +if
    read-key @ +false read-key? !
  +else
    input-fd @ get-nonblocking 0 lit = +if &x-unable-to-read-stdin ?raise +then
    +false input-fd @ set-nonblocking 0 lit = +if
      &x-unable-to-read-stdin ?raise
    +then
    here 1 lit 1 lit allot input-fd @ read dup 0 lit = +if
      &x-unable-to-read-stdin ?raise
    +else -1 lit = +if
     -1 lit allot 1 lit = +if here c@ +else &x-unable-to-read-stdin ?raise +then
    +else
      &x-is-supposed-to-block ?raise
    +then +then
    swap input-fd @ set-nonblocking 0 lit = +if
      &x-unable-to-read-stdin ?raise
    +then
  +then
end-word

\ The size of the terminal input buffer
not-vm 1024 constant tib-size vm

\ The terminal input buffer size constant
non-define-word tib-size ( -- bytes ) tib-size lit end-word

\ The terminal input buffer
define-word-created tib
tib-size 0 set-fill-data

\ The number of characters currently in the terminal input buffer
define-word-created tib#
0 set-cell-data

\ Read terminal input into a buffer
define-word (accept) ( c-addr bytes -- bytes-read )
  0 lit tib# !
  +begin
    tib-size lit tib# @ > +if
      key dup tib tib# @ + c! newline = +if
        true
      +else
        tib# @ 1 lit + tib# ! false
      +then
    +else
      true
    +then
  +until
  dup tib# @ > +if drop tib# @ +then swap tib swap 2 lit pick cmove
end-word

\ The implementation of the BYE word, invoking the BYE service
define-word (bye) ( -- )
  'global-bye-handler @ ?dup +if execute +then cr sys-bye @ sys
end-word

\ The PAUSE hook
define-word-created 'pause
0 set-cell-data

\ The PAUSE wrapper
define-word pause ( -- ) 'pause @ ?dup +if execute +then end-word

\ The default implementation of PAUSE
define-word (pause) ( -- ) end-word

\ Set a number of hooks
define-word set-hooks ( -- )
  &(type) 'type !
  &(key?) 'key? !
  &(key) 'key !
  &(accept) 'accept !
  &(bye) 'bye !
  &(pause) 'pause !
end-word

\ The Forth wordlist constant
define-word forth-wordlist ( -- wid ) 0 lit end-word

\ The maximum number of wordlists
not-vm 256 constant max-wordlist-count vm

\ The array of wordlists' most recent entries
define-word-created wordlist-array
not-vm target-cell-size max-wordlist-count * vm 0 set-fill-data

\ The number of existing wordlist
define-word-created wordlist-count
1 set-cell-data

\ The maximum number of wordlist reached exception
define-word x-max-wordlists ( -- )
  space s" max wordlists reached" +data type cr
end-word

\ The out of range wordlist exception
define-word x-out-of-range-wordlist ( -- )
  space s" out of range wordlist id" +data type cr
end-word

\ Return a new wordlist
define-word wordlist ( -- wid )
  wordlist-count @ max-wordlist-count lit < +if
    0 lit wordlist-array wordlist-count @ cells + !
    wordlist-count @ dup 1 lit + wordlist-count !
  +else
    &x-max-wordlists ?raise
  +then
end-word

\ Get the most recent entry (an xt) in a wordlist, or zero if the wordlist is
\ empty
define-word wordlist>first ( wid -- xt )
  dup wordlist-count @ < +if
    cells wordlist-array + @
  +else
    &x-out-of-range-wordlist ?raise
  +then
end-word

\ Set the most recent entry (an xt) in a wordlist, zero if the wordlist is
\ to be empty
define-word first>wordlist ( xt wid -- )
  dup wordlist-count @ < +if
    cells wordlist-array + !
  +else
    &x-out-of-range-wordlist ?raise
  +then
end-word

\ The wordlist for which words are currently compiled for
define-word-created current-wordlist
0 set-cell-data

\ Set the wordlist for which words are currently compiled for
define-word set-current ( wid -- ) current-wordlist ! end-word

\ Get the wordlist for which words are currently compiled for
define-word get-current ( -- wid ) current-wordlist @ end-word

\ Convert an ASCII character to be upper case
define-word upcase-char ( c -- c )
  dup char a lit >= over char z lit <= and +if char a lit - char A lit + +then
end-word

\ Get whether two equal-length arrays of ASCII characters are equal when
\ differing cases are ignored
define-word equal-case-chars? ( c-addr1 c-addr2 bytes -- matches )
  +begin dup 0 lit > +while
    2 lit pick c@ upcase-char 2 lit pick c@ upcase-char <> +if
      2drop drop +false exit
    +then
    1 lit - rot 1 lit + rot 1 lit + rot
  +repeat
  2drop drop +true
end-word

\ Get whether two equal-length arrays of ASCII characters are equal
define-word equal-chars? ( c-addr1 c-addr2 bytes -- matches )
  +begin dup 0 lit > +while
    2 lit pick c@ 2 lit pick c@ <> +if
      2drop drop +false exit
    +then
    1 lit - rot 1 lit + rot 1 lit + rot
  +repeat
  2drop drop +true
end-word

\ Get whether two names composed of ASCII characters are equal, ignoring case
define-word equal-name? ( c-addr1 bytes1 c-addr2 bytes2 -- matches )
  dup 3 lit pick = +if
    drop swap equal-case-chars?
  +else
    2drop 2drop +false
  +then
end-word

\ Get whether two strings are equal, taking case into account
define-word equal-strings? ( c-addr1 bytes1 c-addr2 bytes2 -- matches )
  dup 3 lit pick = +if
    drop swap equal-chars?
  +else
    2drop 2drop +false
  +then
end-word

\ Get the flags for a word (an xt)
define-word word>flags ( xt -- flags )
  4 lit cells * info-table + @
end-word

\ Set the flags for a word (an xt)
define-word flags>word ( flags xt -- )
  4 lit cells * info-table + !
end-word

\ Get the next word for a word (an xt), or zero if there is no next word
define-word word>next ( xt1 -- xt2 )
  4 lit cells * info-table + cell+ @
end-word

\ Set the next word for a word (an xt), or zero if there is no next word
define-word next>word ( xt1 xt2 -- )
  4 lit cells * info-table + cell+ !
end-word

\ Get the starting address for a word (an xt)
define-word word>start ( xt1 -- xt2 )
  4 lit cells * info-table + 2 lit cells + @
end-word

\ Set the starting address for a word (an xt)
define-word start>word ( xt1 xt2 -- )
  4 lit cells * info-table + 2 lit cells + !
end-word

\ Get the ending address for a word (an xt)
define-word word>end ( xt1 -- xt2 )
  4 lit cells * info-table + 3 lit cells + @
end-word

\ Set the ending address for a word (an xt)
define-word end>word ( xt1 xt2 -- )
  4 lit cells * info-table + 3 lit cells + !
end-word

\ Get the name for a word (an xt)
define-word word>name ( xt -- addr bytes )
  2 lit cells * name-table + dup @ swap cell+ @
end-word

\ Set the name for a word (an xt)
define-word name>word ( addr bytes xt -- )
  2 lit cells * name-table + rot over ! cell+ !
end-word

\ Search a wordlist for a word (an xt) by name, returning a non-zero value and
\ an xt if the word is found in the wordlist, else returning zero and zero
define-word search-wordlist ( c-addr bytes wid -- xt found )
  wordlist>first +begin dup 0 lit <> +while
    dup word>flags hidden-flag lit and 0 lit = +if
      2 lit pick 2 lit pick 2 lit pick word>name equal-name? +if
        rot rot 2drop +true exit
      +then
    +then
    word>next
  +repeat
  2drop drop 0 lit +false
end-word

\ The maximum wordlist order size
not-vm 128 constant max-wordlist-order-count vm

\ The wordlist order array, ordered from first to last wordlist
define-word-created wordlist-order-array
not-vm target-cell-size max-wordlist-order-count * 0 set-fill-data vm

\ The number of entries in the wordlist order
define-word-created wordlist-order-count
1 set-cell-data

\ The maximum wordlist order size reached exception
define-word x-max-wordlist-order-count ( -- )
  space s" max wordlist order count reached" +data type cr
end-word

\ Set the wordlist order with a number of entries in the wordlist combined with
\ that number of wordlist IDs popped off the data stack, in order from first
\ to last
define-word set-order ( widn ... wid1 count -- )
  dup max-wordlist-order-count lit <= +if
    dup wordlist-order-count !
    0 lit +begin 2dup > +while
      rot over cells wordlist-order-array + ! 1 lit +
    +repeat
    2drop
  +else
    &x-max-wordlist-order-count ?raise
  +then
end-word

\ Get the wordlist order with the entries of the wordlist order pushed onto
\ the data stack, in order from last to first, followed by the number of entries
\ in the wordlist order
define-word get-order ( -- widn ... wid1 count )
  wordlist-order-count @ +begin dup 0 lit > +while
    1 lit - dup cells wordlist-order-array + @ swap
  +repeat
  drop wordlist-order-count @
end-word

\ Search all the wordlists, in order from first to last, for a word by name,
\ and if that word is found, it is pushed onto the stack as an xt followed by
\ a non-zero value, else zero followed by zero is pushed onto the data stack
define-word search-wordlists ( c-addr bytes -- xt found )
  0 lit +begin dup wordlist-order-count @ < +while
    dup cell-size * wordlist-order-array + @ 3 lit pick 3 lit pick rot
    search-wordlist +if
      swap drop swap drop swap drop +true exit
    +else
      drop 1 lit +
    +then
  +repeat
  2drop drop 0 lit +false
end-word

\ A pointer to the interpreter input bufer
define-word-created input
0 set-cell-data

\ The number of bytes in the interpreter input buffer
define-word-created input#
0 set-cell-data

\ The current offset in bytes in the interpreter input buffer
define-word-created >in
0 set-cell-data

\ The compile-only word exception
define-word x-compile-only-error ( -- )
  space saved-exception type s" : compile-only word" +data type cr
end-word

\ The parser error (unrecognized data in interpreter input buffer) exception
define-word x-parse-error ( -- )
  space saved-exception type s" : parse error" +data type cr
end-word

\ The no data available to parse in the interpreter input buffer exception
define-word x-no-parse-found ( -- )
  space s" input expected" +data type cr
end-word

\ The parsed name found does not correspond to any word exception
define-word x-no-word-found ( -- )
  space saved-exception type s" : word not found" +data type cr
end-word

\ The no LATESTXT value exists exception
define-word x-no-latestxt ( -- )
  space s" no latestxt exists" +data type cr
end-word

\ Get whether any characters are left in the interpreter input buffer
define-word input-left? ( -- f ) >in @ input# @ < end-word

\ Get whether the current character in the interpreter input buffer is not
\ whitespace
define-word input-not-ws? ( -- f )
  input @ >in @ + c@ dup bl <> over newline <> and swap tab <> and  
end-word

\ Advance the current character in the interpreter input buffer by one byte
define-word advance->in ( -- ) >in @ 1 lit + >in ! end-word

\ Advance the current character in the interpreter input buffer to the next
\ non-whitespace character, and return a non-zero value if a non-whitespace
\ character is found else return zero
define-word advance-to-name-start ( -- found )
  +begin
    input-left? +if
      input-not-ws? +if +true +true +else advance->in +false +then
    +else
      +false +true
    +then
  +until
end-word

\ Advance the current character in the interpreter input buffer to the next
\ whitespace character or the end of the interpreter input buffer, whichever
\ comes first
define-word advance-to-name-end ( -- )
  +begin
    input-left? +if
      input-not-ws? +if advance->in +false +else +true +then
    +else
      +true
    +then
  +until
end-word

\ Parse a name, and return the string found in the interpreter input buffer for
\ it if it is found, else return zero followed by zero
define-word parse-name ( -- c-addr bytes )
  advance-to-name-start +if
    >in @ advance-to-name-end input @ over + >in @ rot -
  +else
    0 lit 0 lit
  +then
end-word

\ Get whether the current character in the interpreter input buffer is a close
\ paren
define-word input-close-paren? ( -- f ) input @ >in @ + c@ char ) lit = end-word

\ Parse a paren comment
non-define-word-immediate ( ( -- )
  +begin
    input-left? +if input-close-paren? advance->in +else +true +then
  +until
end-word

\ Get whether the current character in the interpreter input buffer is a newline
define-word input-newline? ( -- f ) input @ >in @ + c@ newline = end-word

\ Parse a backslash comment
non-define-word-immediate \ ( -- )
  +begin
    input-left? +if input-newline? advance->in +else +true +then
  +until
end-word

\ Get whether the current character in the interpreter input buffer is a quote
define-word input-quote? ( -- f ) input @ >in @ + c@ char " lit = end-word

\ Parse a string closed by a quote and return it on the data stack
define-word parse-string ( "ccc<quote>" -- c-addr bytes )
  advance-to-name-start +if
    input @ >in @ + 0 lit +begin
      input-left? +if
        input-quote? +if +true +else 1 lit + +false +then advance->in
      +else
        +true
      +then
    +until
  +else
    &x-no-parse-found ?raise
  +then
end-word

\ Get the minimum of two signed values
define-word min ( n1 n2 -- n3 ) 2dup > +if swap +then drop end-word

\ Get the maximum of two signed values
define-word max ( n1 n2 -- n3 ) 2dup < +if swap +then drop end-word

\ Zero in interpretation mode, non-zero in compilation mode
define-word-created state
0 set-cell-data

\ The size of a buffer for interpreter S"
not-vm 1024 constant string-buffer-size vm

\ The buffer for interpreter S" usage -  note that only one interpreter S"
\ string may be used at a time
define-word-created string-buffer
string-buffer-size 0 set-fill-data

\ In interpretation mode, save a string into a buffer and push the address and
\ length of bytes of the string onto the stack; in compilation mode, compile
\ an immediately parsed string such that a pointer to the string combined with
\ the length of the string in bytes is pushed onto the stack when the code
\ being compiled is executed
non-define-word-immediate s" ( "ccc<quote>" -- )
  ( Runtime: -- c-addr bytes )
  parse-string state @ +if
    &(data) compile, dup , swap here 2 lit pick cmove
    dup allot &(lit) compile, ,
  +else
    swap string-buffer rot string-buffer-size lit min >r r@ cmove
    string-buffer r>
  +then
end-word

\ Compile an immediately parsed string such that the string is outputted to
\ standard output when the code being compiled is executed
non-define-word-immediate-compile-only ." ( Compile-time "ccc<quote>" -- )
  ( Runtime: -- c-addr bytes )
  parse-string
  &(data) compile, dup , swap here 2 lit pick cmove dup allot &(lit) compile, ,
  &type compile,
end-word

\ Parse a string closed by a close paren and return it on the data stack
define-word parse-paren-string ( "ccc<quote>" -- c-addr bytes )
  advance-to-name-start +if
    input @ >in @ + 0 lit +begin
      input-left? +if
        input-close-paren? +if +true +else 1 lit + +false +then advance->in
      +else
        +true
      +then
    +until
  +else
    &x-no-parse-found ?raise
  +then
end-word

\ Immediately parse a string closed by a close paren and output it to standard
\ output
non-define-word-immediate .( ( Compile-time "ccc<close-paren>" -- )
  ( Runtime: -- c-addr bytes )
  parse-paren-string type
end-word

\ Parse a name and push its first character onto the data stack, discarding the
\ remainder of the name
non-define-word char ( -- c )
  parse-name 0 lit <> +if
    c@
  +else
    drop &x-no-parse-found ?raise
  +then
end-word

\ Immediately parse a name and compile its first character as a literal, so that
\ it will be pushed onto the data stack when the code compiled is executed
define-word-immediate-compile-only [char] ( run-time: -- c )
  ^char token, &(lit) compile, ,
end-word

\ Parse a name and look it up in the wordlists, pushing its xt onto the data
\ stack if it is found and throwing an exception if it is not
define-word ' ( "name" -- xt )
  parse-name dup 0 lit <> +if
    2dup search-wordlists +if
      rot rot 2drop
    +else
      drop save-exception &x-no-word-found ?raise
    +then
  +else
    2drop &x-no-parse-found ?raise
  +then
end-word

\ Raise a specified exception if the value passed in along with the xt is zero
define-word ??raise ( cond xt|0 -- xt|... )
  swap 0 lit = +if ?raise +else drop +then
end-word

\ Immediately parse a name and look it up in the wordlists; if at runtime the
\ value passed in is zero this xt will be used to raise an exception otherwise
\ it is discarded
define-word-immediate-compile-only averts ( Compile-time: "name" -- )
  ( Run-time: asserted -- xt|... )
  &(lit) compile, ' ,  &??raise compile,
end-word

\ Compile a value on the data stack as a literal, so it is pushed onto the data
\ stack when the code compiled is executed at runtme
define-word-immediate-compile-only literal ( Compile-time: x -- )
  ( Run-time: -- x )
  &(lit) compile, ,
end-word

\ Immediately parse a name and look it up in the wordlists, compiling its xt
\ into the code being compiled so it is pushed onto the data stack when it is
\ executed if it is found and throwing an exception if it is not
define-word-immediate-compile-only ['] ( Compile-time: "name" -- )
  ( Run-time: -- xt )
  &(lit) compile, ' ,
end-word

\ Parse a name and look it up in the wordlists, compiling code that compiles
\ its xt into the code being compiled if it is found and throwing an exception
\ if it is not
define-word-immediate-compile-only compile ( Compile-time: "name" -- )
  &(lit) compile, ' , &compile, compile,
end-word

\ Parse a name and look it up in the wordlists, compiling its xt into the code
\ being compiled if it is found and throwing an exception if it is not
define-word-immediate-compile-only [compile] ( Compile-time: "name" -- )
  ' compile,
end-word

\ Get whether a word (as an xt) is immediate
define-word immediate? ( xt -- flag )
  word>flags immediate-flag lit and 0 lit <>
end-word

\ The actual value of LATESTXT
define-word-created latestxt-value
0 set-cell-data

\ The maximum xt value
define-word-created max-xt
0 set-cell-data

\ Allocate space for a string in user space and copy a string into it, pushing
\ the allocated string's address and length in bytes on the data stack
define-word allocate-string ( c-addr bytes -- c-addr bytes )
  here over allot rot 2 lit pick 2 lit pick swap cmove swap
end-word

\ Parse a name and begin compilation of a word with that name, setting the word
\ to have a secondary code pointer to the address in the data stack after
\ where the word's name is copied, setting the word to have a null data
\ pointer, setting the word to be hidden, setting LATESTXT to the word, and
\ adding the word to the current wordlist
non-define-word : ( -- )
  parse-name dup 0 lit <> +if
    allocate-string +true state ! here new-colon dup 3 lit roll 3 lit roll rot
\    S" compiling: " +DATA TYPE 2 LIT PICK 2 LIT PICK TYPE CR
    name>word hidden-flag lit over flags>word dup max-xt ! dup latestxt-value !
    here latestxt-value @ start>word
    get-current wordlist>first over next>word
    get-current first>wordlist
  +else
    2drop &x-no-parse-found ?raise
  +then
end-word

\ Begin compilation of an anonymous word, setting LATESTXT to the word, and
\ pushing the xt for the word onto the data stack
define-word :noname ( -- xt )
  true state ! here new-colon dup max-xt ! dup latestxt-value !
  here latestxt-value @ start>word
  0 lit 0 lit latestxt-value @ name>word
end-word

\ Create a CREATEd word with the specified name, setting the word to have a data
\ pointer to the address in the data stack after where the word's name is
\ copied, setting the word to have a null secondary code pointer, setting the
\ word to be hidden, setting LATESTXT to the word, and adding the word to the
\ current wordlist
define-word create-with-name ( c-addr bytes -- )
  allocate-string here new-create dup 3 lit roll 3 lit roll rot
\  S" creating: " +DATA TYPE 2 LIT PICK 2 LIT PICK TYPE CR
  name>word dup max-xt ! dup latestxt-value !
  get-current wordlist>first over next>word
  get-current first>wordlist
end-word

\ Parse a name and create a CREATEd word with the specified name, setting the
\ word to have a data pointer to the address in the data stack after where the
\ word's name is copied, setting the word to have a null secondary code pointer,
\ setting the word to be hidden, setting LATESTXT to the word, and adding the
\ word to the current wordlist
define-word create ( -- )
  parse-name dup 0 lit <> +if
    create-with-name
  +else
    2drop &x-no-parse-found ?raise
  +then
end-word

\ Get the xt of the most recently created word
define-word latestxt ( -- xt ) latestxt-value @ end-word

\ Specify that the most recently created word is immediate
define-word immediate ( -- )
  latestxt word>flags immediate-flag lit or latestxt flags>word
end-word

\ Specify that the most recently create word is compile-only
define-word compile-only ( -- )
  latestxt word>flags compile-only-flag lit or latestxt flags>word
end-word

\ Finish compilation of a word whose compilation was started with : or :NONAME ,
\ unsetting the hidden flag
non-define-word-immediate-compile-only ; ( -- )
  latestxt +if
    &exit compile, &end compile,
    here latestxt end>word
    latestxt word>flags hidden-flag lit not and latestxt flags>word
    latestxt finish
    +false state !
  +else
    &x-no-latestxt ?raise
  +then
end-word

\ Set the secondary code pointer of LATESTXT to the pointer on the top of the
\ return stack and set the interpretation pointer to the pointer beneath it on
\ the return stack; an exception is raised if LATESTXT is unset
define-word does> ( -- )
  latestxt ?dup +if set-does> +else &x-no-latestxt ?raise +then
end-word

\ Parse a name and create a cell-sized constant whose value is on the data stack
\ with it as its name
define-word constant ( x -- ) create , does> @ end-word

\ Parse a name and create a cell-sized variable initialized to zero with it as
\ its name
define-word variable ( -- ) create 0 lit , end-word

\ Advance the pointer to a string by one byte and subtract one from its length
define-word advance-string ( c-addr1 bytes1 -- c-addr2 bytes2 )
  1 lit - swap 1 lit + swap
end-word

\ Parse the base for a numeric literal
define-word parse-base ( c-addr1 bytes1 -- c-addr2 bytes2 base )
  dup 0 lit > +if
    over c@ dup char $ lit = +if drop advance-string 16 lit
    +else dup char # lit = +if drop advance-string 10 lit
    +else dup char / lit = +if drop advance-string 8 lit
    +else char % lit = +if advance-string 2 lit
    +else base @
    +then +then +then +then
  +else
    base @
  +then
end-word

\ Parse a digit from 0 to 9 in a numeric literal
define-word parse-digit-0-9 ( base c -- digit matches )
  char 0 lit - dup rot < +if +true +else drop 0 lit +false +then
end-word

\ Parse a digit from A to Z in a numeric literal
define-word parse-digit-a-z ( base c -- digit matches )
  char A lit - 10 lit + dup rot < +if +true
  +else drop 0 lit +false +then
end-word

\ Parse a digit in a numeric literal
define-word parse-digit ( base c -- digit matches )
  dup char 0 lit >= over char 9 lit <= and +if
    parse-digit-0-9
  +else
    upcase-char dup char A lit >= over char Z lit <= and +if
      parse-digit-a-z
    +else
      2drop 0 lit +false
    +then
  +then
end-word

\ Parse the digits of a numeric literal
define-word parse-digits ( base c-addr bytes -- n matches )
  0 lit +begin over 0 lit > +while
    3 lit pick 3 lit pick c@ parse-digit +if
      swap 4 lit pick * + rot 1 lit + rot 1 lit - rot
    +else
      2drop 2drop drop 0 lit +false exit
    +then
  +repeat
  swap drop swap drop swap drop +true
end-word

\ Parse a numeric literal including an initial minus sign
define-word parse-number ( c-addr bytes -- n matches )
  parse-base rot rot dup 0 lit u> +if
    over c@ char - lit = +if
      1 lit - swap 1 lit + swap dup 0 lit u> +if
        parse-digits dup +if
	  drop negate +true
	+then
      +else
        2drop drop 0 lit +false
      +then
    +else
      parse-digits
    +then
  +else
    2drop drop 0 lit +false
  +then
end-word

\ Immediately set the state to interpretation mode
define-word-immediate [ ( -- ) +false state ! end-word

\ Immediately set the state to compilation mode
define-word-immediate ] ( -- ) +true state ! end-word

\ Compile an xt unless it is immediate, where then it is executed immediately
define-word compile-word ( c-addr bytes xt -- )
  rot rot 2drop dup word>flags immediate-flag lit and +if
    execute
  +else
    compile,
  +then
end-word

\ Interpret an xt unless it is compile-only, where then an exception is raised
define-word interpret-word ( c-addr bytes xt -- )
  dup word>flags compile-only-flag lit and +if
    drop save-exception &x-compile-only-error ?raise
  +else
    rot rot 2drop execute
  +then
end-word

\ Parse a number, and in compilation mode compile it as a literal, unless it
\ cannot be parsed, where then an exception is raised
define-word handle-number ( c-addr bytes -- )
  2dup parse-number +if
    rot rot 2drop
    state @ +if
      &(lit) compile, ,
    +then
  +else
    drop save-exception &x-parse-error ?raise
  +then
end-word

\ Data stack underflow exception
define-word x-data-stack-underflow ( -- )
  space s" data stack underflow" +data type cr
end-word

\ Return stack underflow exception
define-word x-return-stack-underflow ( -- )
  space s" return stack underflow" +data type cr
end-word

\ Flag indicating whether PAUSE is called for every word that is interpreted
define-word-created pause-on-interpret
true set-cell-data

\ Interpret every word in the interpretation buffer, unless an exception is
\ raised or data stack or return stack underflow occur; if PAUSE-ON-INTERPRET
\ is set PAUSE is called for each word that is interpreted
define-word interpret ( -- )
  +begin
    parse-name dup 0 lit u> +if
      2dup search-wordlists +if
        state @ +if
	  compile-word
	+else
          interpret-word
	+then
	sp@ sbase @ > +if &x-data-stack-underflow ?raise +then
	rp@ rbase @ > +if &x-return-stack-underflow ?raise +then
	+false
      +else
        drop handle-number +false
      +then
    +else
      drop drop +true
    +then
    pause-on-interpret @ +if pause +then
  +until
end-word

\ Evaluate code in a string by pushing the current interpretation buffer state
\ onto the data stack, pushing the string as the new interpretation buffer,
\ interpreting the code, restoring the interpretation buffer state (even if any
\ exceptions occur), and re-raising any exceptions that may have occurred
define-word evaluate ( c-addr bytes -- )
  input @ >r input# @ >r >in @ >r
  0 lit >in ! input# ! input !
  &interpret try r> >in ! r> input# ! r> input ! ?dup +if ?raise +then
end-word

\ This is the interpretation buffer used for code gotten from the terminal
\ input buffer, to enable using ACCEPT while interpreting code read from the
\ terminal
define-word-created input-buffer
tib-size 0 set-fill-data

\ This is the number of bytes in the interpretation buffer used for code gotten
\ from the terminal input buffer
define-word-created input-buffer#
0 set-cell-data

\ Refill the interpretation buffer when input is set to the terminal
define-word refill-terminal ( -- )
  input-buffer tib-size lit accept dup input-buffer# ! input# !
  input-buffer input ! 0 lit >in !
end-word

\ Refill hook
define-word-created 'refill
0 set-cell-data

\ Refill wrapper
define-word refill ( -- ) 'refill @ ?dup +if execute +then end-word

\ Abort exception
define-word abort-exception ( -- ) space s" aborted" +data type cr end-word

\ Quit exception
define-word quit-exception ( -- ) space s" quit" +data type cr end-word

\ Pointer to receive address of Forth code bundled in with a VM image
define-word-created storage
0 set-cell-data

\ Pointer to receive number of bytes of Forth code bundled in with a VM image
define-word-created storage#
0 set-cell-data

\ Execute an xt with single-tasking IO set to TRUE
define-word as-non-task-io ( xt -- )
  single-task-io @ >r +true single-task-io ! try r> single-task-io ! ?raise
end-word

\ Execute Forth code bundled in with a VM image
define-word interpret-storage ( -- )
  input @ >r input# @ >r >in @ >r
  0 lit >in ! storage# @ input# ! storage @ input !
  &interpret try r> >in ! r> input# ! r> input ! ?dup +if
    dup &quit-exception = +if
      as-non-task-io +false state !
    +else
      as-non-task-io sbase @ sp! +false state !
    +then
  +then
end-word

\ The main outer interpreter
define-word outer ( -- )
  rbase @ rp!
  +begin
    refill
    &interpret try dup 0 lit = +if
      drop state @ 0 lit = input @ input-buffer = and +if
        space s" ok" +data type cr
      +then
    +else
      dup &quit-exception = +if
        as-non-task-io rbase @ rp! +false state !
      +else
        as-non-task-io sbase @ sp! rbase @ rp! +false state !
      +then
    +then
    pause
  +again
end-word

\ Raise an abort exception
define-word abort &abort-exception ?raise end-word

\ Raise a quit exception
define-word quit &quit-exception ?raise end-word

\ The user variable count
define-word-created #user
not-vm build-#user @ set-cell-data vm

\ Parse a name and create a marker named by it, which when executed will erase
\ everything defined after and including itself, including restoring wordlists
\ to the state where they were when the marker was defined
define-word marker ( "name" -- )
  max-xt @ latestxt here create , , , #user @ , wordlist-count @ ,
  wordlist-array wordlist-count @ +begin dup 0 lit > +while
    swap dup @ dup latestxt = +if word>next +then , cell+ swap 1 lit -
  +repeat
  2drop wordlist-order-count @ ,
  wordlist-order-array wordlist-order-count @ +begin dup 0 lit > +while
    swap dup @ , cell+ swap 1 lit -
  +repeat
  2drop get-current ,
  does>
  dup @ here!
  cell+ dup @ latestxt-value !
  cell+ dup @ dup max-xt ! 1 lit + set-word-count cell+
  dup @ #user ! cell+ dup @ dup wordlist-count !
  swap cell+ swap wordlist-array swap +begin dup 0 lit > +while
    swap rot dup @ rot tuck ! cell+ swap cell+ swap rot 1 lit -
  +repeat
  2drop dup @ dup wordlist-order-count !
  swap cell+ swap wordlist-order-array swap +begin dup 0 lit > +while
    swap rot dup @ rot tuck ! cell+ swap cell+ swap rot 1 lit -
  +repeat
  2drop @ set-current
end-word

\ Relocate the name table upon loading
define-word relocate-name-table ( end-token -- )
  0 lit +begin 2dup >= +while
    name-table over 2 lit cells * name-table + +! 1 lit +
  +repeat
  2drop
end-word

\ Relocate the info table upon loading
define-word relocate-info-table ( end-token -- )
  0 lit +begin 2dup >= +while
    name-table over 4 lit cells * 2 lit cells + info-table + +!
    name-table over 4 lit cells * 3 lit cells + info-table + +! 1 lit +
  +repeat
  2drop
end-word

\ Exception-protected boot actions
define-word boot-protected ( -- )
  lookup-sys name-table set-name-table
  set-hooks &refill-terminal 'refill ! interpret-storage outer
end-word

\ The minimum terminal task user variable count
not-vm 32 constant min-terminal-task-user-count vm

\ The terminal task user variable count
define-word terminal-task-user-count
  #user @ min-terminal-task-user-count lit > +if
    #user @
  +else
    min-terminal-task-user-count lit
  +then
end-word

\ The initial entry point
define-word boot ( storage storage-size here -- )
  dup up ! user-space-current !
  terminal-task-user-count cells allot
  storage# ! storage ! 10 lit base ! 0 lit handler !
  sp@ sbase ! rp@ rbase !
  main-format-digit-buffer format-digit-buffer !
  input-buffer input ! 0 lit input-buffer# ! 0 lit input# !
  stdin input-fd ! stdout output-fd ! stderr error-fd !
  0 lit read-key ! +false read-key? !
  &boot latestxt-value !
  &boot relocate-name-table
  &boot relocate-info-table
  &boot wordlist-array 0 lit cells + ! &boot-protected try ?dup +if
    s" error " +data output-fd @ write 2drop bye
  +else
    bye
  +then
end-word

\ Forth files to bundle in with the assembled VM
s" src/hashforth/startup.fs" add-source-to-storage
s" src/hashforth/lambda.fs" add-source-to-storage
s" src/hashforth/task.fs" add-source-to-storage
s" src/hashforth/allocate.fs" add-source-to-storage
s" src/hashforth/allocate_task.fs" add-source-to-storage
s" src/hashforth/buffer.fs" add-source-to-storage
s" src/hashforth/io.fs" add-source-to-storage
s" src/hashforth/cond.fs" add-source-to-storage
s" src/hashforth/mutex.fs" add-source-to-storage
s" src/hashforth/bchan.fs" add-source-to-storage
s" src/hashforth/bufbchan.fs" add-source-to-storage
s" src/hashforth/varbchan.fs" add-source-to-storage
s" src/hashforth/vector.fs" add-source-to-storage
s" src/hashforth/intmap.fs" add-source-to-storage
s" src/hashforth/map.fs" add-source-to-storage
s" src/hashforth/bufchan.fs" add-source-to-storage
s" src/hashforth/varchan.fs" add-source-to-storage
s" src/hashforth/line.fs" add-source-to-storage
s" src/hashforth/ready.fs" add-source-to-storage
