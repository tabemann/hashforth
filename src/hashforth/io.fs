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

get-order get-current base @

decimal
forth-wordlist task-wordlist buffer-wordlist 3 set-order
forth-wordlist set-current

\ Read-only file opening flag
1 constant open-rdonly

\ Write-only file opening flag
2 constant open-wronly

\ Read/write file opening flag
4 constant open-rdwr

\ Appending file opening flag
8 constant open-append

\ Creating file opening flag
16 constant open-creat

\ Exclusive create (to be combined with OPEN-CREAT) file opening flag
32 constant open-excl

\ Truncating file opening flag
64 constant open-trunc

\ Services for opening and closing files.
variable sys-open
variable sys-close
variable sys-prepare-terminal
variable sys-cleanup-terminal
variable sys-get-terminal-size

\ Call service for opening files; returns -1 on success and 0 on failure
: open ( c-addr bytes flags mode -- fd -1|0 ) sys-open @ sys ;

\ Call service for closing files; returns -1 on success and 0 on failure
: close ( fd -- -1|0 ) sys-close @ sys ;

\ Call service for preparing terminals; returns -1 on success and 0 on failure
: prepare-terminal ( fd -- -1|0 ) sys-prepare-terminal @ sys ;

\ Call service for cleaning up terminals; returns -1 on success and 0 on failure
: cleanup-terminal ( fd -- -1|0 ) sys-cleanup-terminal @ sys ;

\ Call service to get the terminal size; returns -1 on success and 0 on failure
: get-terminal-size ( fd -- rows columns xpixels ypixels -1|0 )
  sys-get-terminal-size @ sys ;

\ Hook for preparing to read
variable 'prepare-read

\ Wrapper for preparing to read
: prepare-read ( fd -- ) 'prepare-read @ ?dup if execute else drop then ;

\ Actually wait for and read a file descriptor (returns -1 on success and 0 on
\ error).
: (wait-read) ( buf bytes fd -- bytes-read -1|0 )
  dup set-wait-in begin    
    pause
    2 pick 2 pick 2 pick read dup true = if
      drop swap drop swap drop swap drop true true
    else 1 = if
      drop false
    else
      2drop 2drop 0 false true
    then then
  until
  unset-wait-in ;

\ Wait for and read a file descriptor (returns -1 on success and 0 on error).
: wait-read ( buf bytes fd -- bytes-read -1|0 )
  dup get-nonblocking drop >r >r r@ true over set-nonblocking if
    2 pick 2 pick 2 pick
    read dup true = if
      drop swap drop swap drop swap drop true
    else 1 = if
      drop (wait-read)
    else
      2drop 2drop 0
    then then
  else
    drop 2drop 0
  then
  r> r> swap set-nonblocking 0 = if 2drop 0 0 then ;

\ Attempt to fully read a buffer of data from a file descriptor (returns -1 on
\ success and 0 on error).
: wait-read-full ( buf bytes fd -- bytes-read -1|0 )
  in-task? averts x-not-in-task
  dup prepare-read
  0 begin
    3 pick 3 pick 3 pick wait-read if
      dup 0 = if
        drop -1 true
      else
        tuck + 4 roll 4 roll 3 roll advance-buffer 3 roll 3 roll
        2 pick 0 = if -1 true else false then
      then
    else
      drop 0 true
    then
  until
  rot drop rot drop rot drop ;

\ Wait for and read a file descriptor (returns -1 on success and 0 on error).
: wait-read ( buf bytes fd -- bytes-read -1|0 )
  in-task? averts x-not-in-task dup prepare-read wait-read ;

\ Actually wait for and write a file descriptor (returns -1 on success and 0 on
\ error).
: (wait-write) ( buf bytes fd -- bytes-written -1|0 )
  dup set-wait-out begin
    pause
    2 pick 2 pick 2 pick write dup true = if
      drop swap drop swap drop swap drop true true
    else 1 = if
      false
    else
      2drop 2drop 0 false true
    then then
  until
  unset-wait-out ;

\ Wait for and write a file descriptor (returns -1 on success and 0 on error).
: wait-write ( buf bytes fd -- bytes-written -1|0 )
  in-task? averts x-not-in-task
  dup get-nonblocking if
    >r >r r@ true over set-nonblocking if
      2 pick 2 pick 2 pick write dup true = if
        drop swap drop swap drop swap drop true
      else 1 = if
        drop (wait-write)
      else
        2drop 2drop 0 0
      then then
    else
      drop 2drop 0 0
    then
  else
    drop 2drop 0 0
  then
  r> r> swap set-nonblocking 0 = if 2drop 0 0 then ;

\ Attempt to fully write a buffer of data from a file descriptor (returns -1 on
\ success and 0 on error).
: wait-write-full ( buf bytes fd -- bytes-write -1|0 )
  in-task? averts x-not-in-task
  0 begin
    3 pick 3 pick 3 pick wait-write if
      tuck + 4 roll 4 roll 3 roll advance-buffer 3 roll 3 roll
      2 pick 0 = if -1 true else false then
    else
      drop 0 true
    then
  until
  rot drop rot drop rot drop ;

\ Implement TYPE
: (type) ( c-addr bytes -- )
  in-task? single-task-io @ not and if
    output-fd @ wait-write-full averts x-unable-to-write-stdout drop
  else
    (type)
  then ;

\ Test whether a key is read.
: (key?) ( -- flag )
  in-task? single-task-io @ not and if
    read-key? @ if
      true
    else
      input-fd @ get-nonblocking averts x-unable-to-read-stdin
      true input-fd @ set-nonblocking averts x-unable-to-read-stdin
      here 1 1 allot input-fd @ read dup 0<> averts x-unable-to-read-stdin
      -1 = if
        -1 allot 1 = if here c@ read-key ! true read-key? ! true
        else ['] x-unable-to-read-stdin ?raise then
      else
        -1 allot drop false
      then
      swap input-fd @ set-nonblocking averts x-unable-to-read-stdin
    then
  else
    (key?)
  then ;

\ Read a keypress from standard input.
: (key) ( -- c )
  in-task? single-task-io @ not and if
    read-key? @ if
      read-key @ false read-key? !
    else
      here 1 1 allot input-fd @ wait-read-full averts x-unable-to-read-stdin
      -1 allot 1 = if here c@ else bye then
    then
  else
    (key)
  then ;

\ Buffer size for reading data into memory
1024 constant read-buffer-size
create read-buffer read-buffer-size allot

\ Read a whole file into memory in a buffer; returns -1 on success and 0 on
\ failure.
: read-file-into-buffer ( c-addr bytes buffer -- -1|0 )
  rot rot open-rdonly /777 open if
    begin
      read-buffer read-buffer-size 2 pick wait-read-full if
        dup 0 = if
          drop -1 true
        else
          read-buffer swap 3 pick append-buffer false
        then
      else
        drop 0 true
      then
    until
    swap close drop swap drop
  else
    drop drop 0
  then ;

\ Unable to load Forth code exception.
: x-code-loading-failure space ." unable to load code" cr ;

\ Default included code buffer size
1024 constant included-buffer-size

\ Linked list item for previously included source files
begin-structure included-item-size
  field: included-item-next
  field: included-item-name-len
end-structure

\ Included source file name
: included-item-name ( addr -- ) included-item-size + ;

\ First included item
variable first-included-item
 
\ Allocate an included item
: include-item ( c-addr bytes -- )
  included-item-size allocate! first-included-item @ over included-item-next !
  2dup included-item-name-len !
  rot rot 2 pick included-item-name swap cmove
  first-included-item ! ;

\ Get whether a file has already been included
: included? ( c-addr bytes -- included-flag )
  first-included-item begin
    dup if
      dup included-item-name over included-item-name-len @
      4 pick 4 pick equal-strings? if
        drop 2drop true true
      else
        included-item-next @ false
      then
    else
      drop 2drop false true
    then
  until ;

\ Add an included item if it is not already included.
: check-include-item ( c-addr bytes -- )
  2dup included? if include-item else 2drop then ;

\ Load Forth code from a file.
: included ( c-addr bytes -- )
  2dup check-include-item
  included-buffer-size allocate-buffer
  rot rot 2 pick read-file-into-buffer averts x-code-loading-failure
  dup get-buffer evaluate destroy-buffer ;

\ Load Forth code from a file specified in interpretation mode.
: include ( "file" -- ) parse-name included ; immediate

\ Load Forth code from a file if it is not already loaded.
: required ( c-addr bytes -- ) 2dup included? not if included else 2drop then ;

\ Load Forth code from a file specified in interpretation mode if it is not
\ already loaded.
: require ( "file" -- ) parse-name required ; immediate

\ Execute code with a specific input file descriptor
: with-input-fd ( xt fd -- )
  input-fd @ >r input-fd ! try r> input-fd ! ?raise ;

\ Execute code with a specific output file descriptor
: with-output-fd ( xt fd -- )
  output-fd @ >r output-fd ! try r> output-fd ! ?raise ;

\ Execute code with a specific error file descriptor
: with-error-fd ( xt fd -- )
  error-fd @ >r error-fd ! try r> error-fd ! ?raise ;

\ Old BYE implementation
variable old-bye 'bye @ old-bye !

\ New BYE implementation
: (bye) ( -- ) input-fd @ cleanup-terminal old-bye @ execute ;

\ Initialize IO
: init-io ( -- )
  s" OPEN" sys-lookup sys-open !
  s" CLOSE" sys-lookup sys-close !
  s" PREPARE-TERMINAL" sys-lookup sys-prepare-terminal !
  s" CLEANUP-TERMINAL" sys-lookup sys-cleanup-terminal !
  s" GET-TERMINAL-SIZE" sys-lookup sys-get-terminal-size !
  ['] (bye) 'bye !
  ['] (type) 'type !
  ['] (key?) 'key? !
  ['] (key) 'key !
  ['] (accept) 'accept ! ;

init-io

base ! set-current set-order
