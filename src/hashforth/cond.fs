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
forth-wordlist task-wordlist 2 set-order
task-wordlist set-current

begin-structure cond-size
  field: cond-waiting
  field: cond-waiting-count
  field: cond-waiting-size
end-structure

\ Allocate a new condition variable with a specified waiting queue size.
: new-cond ( waiting-size -- cond )
  here cond-size allot
  tuck cond-waiting-size !
  0 over cond-waiting-count !
  dup cond-waiting-size @ here swap cells allot over cond-waiting ! ;

\ Wait on a condition variable.
: wait-cond ( cond -- )
  begin dup cond-waiting-count @ over cond-waiting-size @ = while
    pause
  repeat
  dup cond-waiting @ over cond-waiting-count @ cells + current-task swap !
  1 swap cond-waiting-count +!
  current-task deactivate-task ;

\ Signal a condition variable.
: signal-cond ( cond -- )
  dup cond-waiting-count @ 0 > if
    dup cond-waiting @ @ activate-task
    dup cond-waiting @ cell+ over cond-waiting @
    2 pick cond-waiting-count @ 1 - cells move
    -1 swap cond-waiting-count +!
  else
    drop
  then ;

\ Broadcast on a condition variable.
: broadcast-cond ( cond -- )
  dup cond-waiting-count @ 0 ?do
    dup cond-waiting @ i cells + @ activate-task
  loop
  0 swap cond-waiting-count ! ;

base ! set-current set-order
