\ Copyright (c) 2018-2019, Travis Bemann
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
forth-wordlist lambda-wordlist task-wordlist 3 set-order
task-wordlist set-current

begin-structure mutex-size
  field: mutex-flags
  field: mutex-held
  field: mutex-waiting
  field: mutex-waiting-count
  field: mutex-waiting-size
end-structure

\ Mutex is allocated flags
1 constant allocated-mutex

\ Dump a mutex
: mutex. ( mutex -- )
  [:
    cr ." mutex-flags: " dup mutex-flags @ .
    cr ." mutex-held: " dup mutex-held @ .
    cr ." mutex-waiting: " dup mutex-waiting @ .
    cr ." mutex-waiting-count: " dup mutex-waiting-count @ .
    cr ." mutex-waiting-size: " mutex-waiting-size @ .
  ;] as-non-task-io ;

\ Allot a new mutex variable with a specified waiting queue size.
: allot-mutex ( waiting-size -- mutex )
  here mutex-size allot
  tuck mutex-waiting-size !
  0 over mutex-flags !
  0 over mutex-waiting-count !
  0 over mutex-held !
  dup mutex-waiting-size @ here swap cells allot over mutex-waiting ! ;

\ Allocate a new mutex variable with a specified waiting queue size.
: allocate-mutex ( waiting-size -- mutex )
  mutex-size allocate!
  tuck mutex-waiting-size !
  allocated-mutex over mutex-flags !
  0 over mutex-waiting-count !
  0 over mutex-held !
  dup mutex-waiting-size @ cells allocate! over mutex-waiting ! ;

\ Mutex is not allocated exception
: x-mutex-not-allocated ( -- ) space ." mutex is not allocated" cr ;

\ Destroy a mutex
: destroy-mutex ( mutex -- )
  dup mutex-flags @ allocated-mutex and averts x-mutex-not-allocated
  dup mutex-waiting @ free! free! ;

\ Lock a mutex.
: lock-mutex ( mutex -- )
  begin dup mutex-held @ 0<> over mutex-held @ current-task <> and while
    begin dup mutex-waiting-count @ over mutex-waiting-size @ = while
      pause
    repeat
    dup mutex-held @ 0<> over mutex-held @ current-task <> and if
      dup mutex-waiting @ over mutex-waiting-count @ cells + swap !
      1 over mutex-waiting-count +!
      current-task deactivate-task
    then
  repeat
  current-task swap mutex-held ! ;

\ Unlock a mutex.
: unlock-mutex ( mutex -- )
  dup mutex-held @ current-task = if
    dup mutex-waiting-count @ 0 > if
      dup mutex-waiting @ @ 2dup mutex-held ! activate-task
      dup mutex-waiting @ cell+ over mutex-waiting @
      2 pick mutex-waiting-count @ 1 - cells move
      -1 swap mutex-waiting-count +!
    else
      0 swap mutex-held !
    then
  then ;

\ Lock and unlock a mutex after executing a word.
: with-mutex ( mutex xt -- )
  over lock-mutex swap >r try r> unlock-mutex ?reraise ;

base ! set-current set-order