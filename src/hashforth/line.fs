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
forth-wordlist 1 set-order
forth-wordlist set-current

wordlist constant line-wordlist

\ Character constants
$01 constant ctrl-a
$02 constant ctrl-b
$04 constant ctrl-d
$05 constant ctrl-e
$06 constant ctrl-f
$09 constant tab
$0b constant ctrl-k
$0c constant ctrl-l
$17 constant ctrl-w
$19 constant ctrl-y
$1b constant escape
$7f constant delete

\ Command type constants
0 constant normal-command
1 constant kill-forward-command
2 constant kill-backward-command
3 constant yank-command

forth-wordlist vector-wordlist line-wordlist 3 set-order
line-wordlist set-current

tib-size constant line-history-data-size

begin-structure line-history-header-size
  field: line-history-entry-length
end-structure

line-history-header-size line-history-data-size +
constant line-history-item-size

tib-size constant line-kill-data-size

begin-structure line-kill-header-size
  field: line-kill-entry-length
end-structure

line-kill-header-size line-kill-data-size +
constant line-kill-item-size

128 constant line-history-size
128 constant line-kill-size

begin-structure line-size
  field: line-offset
  field: line-history-vector
  field: line-history-edits-vector
  field: line-history-current
  field: line-kill-vector
  field: line-buffer
  field: line-count
  field: line-start-row
  field: line-start-column
  field: line-terminal-rows
  field: line-terminal-columns
  field: line-show-cursor-count
  field: line-last-command-type
  field: line-first-line-added
  field: line-complete-enable
end-structure

\ Terminal structure for the current task
user line

\ Unable to prepare a file descriptor
: x-unable-to-prepare-terminal ( -- ) space ." unable to prepare terminal" cr ;

\ Prepare for reading from a file descriptor
: (prepare-read) ( fd -- )
  input-fd @ = if
    input-fd @ prepare-terminal averts x-unable-to-prepare-terminal
  then ;

\ Set the prepare read handler
' (prepare-read) 'prepare-read !

\ Initialize a line editor
: new-line ( -- editor )
  here line-size allot
  0 over line-offset !
  line-history-size line-history-item-size allot-vector over
  line-history-vector !
  line-history-size line-history-item-size allot-vector over
  line-history-edits-vector !
  -1 over line-history-current !
  line-kill-size line-kill-item-size allot-vector over line-kill-vector !
  here tib-size allot over line-buffer !
  0 over line-count !
  0 over line-start-row !
  0 over line-start-column !
  0 over line-terminal-rows !
  0 over line-terminal-columns !
  0 over line-show-cursor-count !
  normal-command over line-last-command-type !
  true over line-complete-enable ! ;

\ Set normal command type
: set-normal-command ( -- ) normal-command line @ line-last-command-type ! ;

\ Set kill forward command type
: set-kill-forward-command ( -- )
  kill-forward-command line @ line-last-command-type ! ;

\ Set kill backward command type
: set-kill-backward-command ( -- )
  kill-backward-command line @ line-last-command-type ! ;

\ Set yank command type
: set-yank-command ( -- ) yank-command line @ line-last-command-type ! ;

\ Type a decimal integer
: (dec.) ( n -- ) 10 (base.) ;

\ Type the CSI sequence
: csi ( -- ) begin-atomic escape emit [char] [ emit end-atomic ;

\ Show the cursor
: show-cursor ( -- )
  begin-atomic csi [char] ? emit 25 (dec.) [char] h emit end-atomic ;

\ Hide the cursor
: hide-cursor ( -- )
  begin-atomic csi [char] ? emit 25 (dec.) [char] l emit end-atomic ;

\ Save the cursor position
: save-cursor ( -- ) begin-atomic csi [char] s emit end-atomic ;

\ Restore the cursor position
: restore-cursor ( -- ) begin-atomic csi [char] u emit end-atomic ;

\ Scroll up screen by one line
: scroll-up ( lines -- ) begin-atomic csi (dec.) [char] S emit end-atomic ;

\ Move the cursor to specified row and column coordinates
: go-to-coord ( row column -- )
  begin-atomic
  swap csi 1+ (dec.) [char] ; emit 1+ (dec.) [char] f emit
  end-atomic ;

\ Erase from the cursor to the end of the line
: erase-end-of-line ( -- ) begin-atomic csi [char] K emit end-atomic ;

\ Erase the lines below the current line
: erase-down ( -- ) begin-atomic csi [char] J emit end-atomic ;

\ Query for the cursor position
: query-cursor-position ( -- )
  begin-atomic csi [char] 6 emit [char] n emit end-atomic ;

\ Show the cursor with a show/hide counter
: show-cursor ( -- )
  1 line @ line-show-cursor-count +!
  line @ line-show-cursor-count @ 0 = if show-cursor then ;

\ Hide the cursor with a show/hide counter
: hide-cursor ( -- )
  -1 line @ line-show-cursor-count +!
  line @ line-show-cursor-count @ -1 = if hide-cursor then ;

\ Execute code with a hidden cursor
: execute-hide-cursor ( xt -- ) hide-cursor try show-cursor ?raise ;

\ Execute code while preserving cursor position
: execute-preserve-cursor ( xt -- ) save-cursor try restore-cursor ?raise ;

\ Wait for a number
: wait-number ( -- n matches )
  here >r key dup [char] - = if here c! 1 allot else set-key then
  0 begin
    dup 64 < if
      key dup [char] 0 >= over [char] 9 <= and if
        here c! 1 allot 1 + false
      else
        set-key true
      then
    else
      true
    then
  until
  drop
  r@ here r@ - ['] parse-number 10 base-execute r> here! ;

\ Wait for a character
: wait-char ( c -- ) begin dup key = until drop ;

\ Confirm that a character is what is expected
: expect-char ( c -- flag ) key dup rot = if drop true else set-key false then ;

\ Get the cursor position
: get-cursor-position ( -- row column )
  begin
    clear-key query-cursor-position escape wait-char
    [char] [ expect-char if
      wait-number if
        1 - [char] ; expect-char if
	  wait-number if
	    [char] R expect-char if
	      1 - true
	    else
	      2drop false
	    then
	  else
	    drop false
	  then
	else
	  drop false
	then
      else
        drop false
      then
    else
      false
    then
  until ;

\ Get the cursor position with a hidden cursor
: get-cursor-position ( -- row column )
  ['] get-cursor-position execute-hide-cursor ;

\ Update the start position
: update-start-position ( -- )
  get-cursor-position line @ line-start-column ! line @ line-start-row ! ;

\ Unable to get terminal size exception
: x-unable-to-get-terminal-size ( -- )
  space ." unable to get terminal size" cr ;

\ Update the terminal size
: update-terminal-size ( -- )
  input-fd @ get-terminal-size averts x-unable-to-get-terminal-size
  2drop line @ line-terminal-columns ! line @ line-terminal-rows !
  line @ line-start-column @ line @ line-terminal-columns @ >= if
    0 line @ line-start-column ! 1 line @ line-start-row +!
    line @ line-start-row @ line @ line-terminal-rows @ >= if
      line @ line-terminal-rows @ 1 - line @ line-start-row !
    then
  then ;

\ Get the start position
: start-position ( -- row column )
  line @ line-start-row @ line @ line-start-column @ ;

\ Get the position at a character offset
: offset-position ( offset -- row column )
  line @ line-start-column @ +
  dup line @ line-terminal-columns @ / line @ line-start-row @ +
  swap line @ line-terminal-columns @ mod ;

\ Get the cursor position
: cursor-position ( -- row column )
  line @ line-offset @ offset-position ;

\ Get the end position
: end-position ( -- row column ) line @ line-count @ offset-position ;

\ Calculate number of lines text will take up
: total-lines ( -- lines )
  line @ line-count @ line @ line-start-column @ +
  line @ line-terminal-columns @ / 1 + ;

\ Adjust start row
: adjust-start-row ( -- )
  line @ line-terminal-rows @ total-lines line @ line-start-row @ + - dup 0 < if
    dup line @ line-start-row +! negate scroll-up
  else
    drop
  then ;

\ Actually update the line editor
: update-line ( -- )
  adjust-start-row
  start-position go-to-coord line @ line-buffer @ line @ line-count @ type
  end-position go-to-coord erase-down erase-end-of-line
  cursor-position go-to-coord ;

\ Update the line editor
: update-line ( -- ) ['] update-line execute-hide-cursor ;

\ Reset the line editor state for a new line of input
: reset-line ( -- ) 0 line @ line-offset ! 0 line @ line-count ! ;

\ Actually configure the line editor state for a new line of input
: config-line ( -- )
  false line @ line-first-line-added ! -1 line @ line-history-current !
  update-start-position update-terminal-size ;

\ Configure the line editor state for a new line of input
: config-line ( -- ) ['] config-line execute-hide-cursor ;

\ Find the next non-whitespace character
: find-next-print ( offset1 -- offset2 )
  begin
    dup line @ line-count @ < if
      dup line @ line-buffer @ + c@ dup bl = swap tab = or not
      dup not if swap 1 + swap then
    else
      true
    then
  until ;

\ Find the next whitespace character
: find-next-ws ( offset1 -- offset2 )
  begin
    dup line @ line-count @ < if
      dup line @ line-buffer @ + c@ dup bl = swap tab = or
      dup not if swap 1 + swap then
    else
      true
    then
  until ;

\ Find the previous non-whitespace character
: find-prev-print ( offset1 -- offset2 )
  begin
    dup 0 > if
      dup line @ line-buffer @ + c@ dup bl = swap tab = or not
      dup not if swap 1 - swap then
    else
      true
    then
  until ;

\ Find the previous whitespace character
: find-prev-ws ( offset1 -- offset2 )
  begin
    dup 0 > if
      dup line @ line-buffer @ + c@ dup bl = swap tab = or
      dup not if swap 1 - swap then
    else
      true
    then
  until ;

\ Find the last previous non-whitespace character
: find-prev-last-print ( offset1 -- offset2 )
  begin
    dup 0 > if
      dup 1 - line @ line-buffer @ + c@ dup bl = swap tab = or
      dup not if swap 1 - swap then
    else
      true
    then
  until ;

\ Find the last previous whitespace character
: find-prev-last-ws ( offset1 -- offset2 )
  begin
    dup 0 > if
      dup 1 - line @ line-buffer @ + c@ dup bl = swap tab = or not
      dup not if swap 1 - swap then
    else
      true
    then
  until ;

\ Generic history exception
: x-history-internal ( -- ) space ." internal history failure" cr ;

\ Generic kill exception
: x-kill-internal ( -- ) space ." internal kill failure" cr ;

\ Actually add a line to history
: add-to-history ( -- )
  line @ line-history-vector @ count-vector line-history-size = if
    line @ line-history-vector @ drop-end-vector averts x-history-internal
    line @ line-history-edits-vector @ drop-end-vector averts x-history-internal
  then
  here line @ line-count @ over line-history-entry-length !
  line @ line-buffer @ over line-history-header-size + tib-size move
  line-history-item-size allot
  dup line @ line-history-vector @ push-start-vector averts x-history-internal
  line @ line-history-edits-vector @ push-start-vector averts x-history-internal
  line-history-item-size negate allot ;

\ Set added first line
: set-added-first-line-in-history ( -- )
  here line-history-item-size allot
  line @ line-buffer @ over line-history-header-size +
  line @ line-count @ move line @ line-count @ over !
  0 line @ line-history-vector @ set-vector line-history-item-size negate allot ;

\ Compare and add first line
: compare-add-to-history ( -- )
  here line-history-item-size allot
  dup 0 line @ line-history-vector @ get-vector averts x-history-internal
  dup line-history-header-size + swap line-history-entry-length @
  line @ line-buffer @ line @ line-count @ equal-strings?
  line-history-item-size negate allot not if add-to-history then ;

\ Remove the first line if empty
: remove-first-line-if-empty ( -- )
  here line-history-item-size allot
  dup 0 line @ line-history-vector @ get-vector averts x-history-internal
  line-history-entry-length @ 0 = if
    false line @ line-first-line-added !
    line @ line-history-vector @ drop-start-vector averts x-history-internal
  then
  line-history-item-size negate allot ;

\ Add a line to history
: add-to-history ( -- )
  line @ line-first-line-added @
  line @ line-history-vector @ count-vector 1 > and if
    remove-first-line-if-empty
  then
  line @ line-count @ 0 > if
    line @ line-first-line-added @ line @ line-history-current @ 0 = and if
      set-added-first-line-in-history
    else line @ line-history-vector @ count-vector 0 > if
      compare-add-to-history
    else
      add-to-history
    then then
  then ;

\ Revert history edits
: revert-history-edits ( -- )
  line @ line-history-vector @ count-vector 0 ?do
    here line-history-item-size allot
    dup i line @ line-history-vector @ get-vector averts x-history-internal
    i line @ line-history-edits-vector @ set-vector averts x-history-internal
    line-history-item-size negate allot
  loop ;

\ Handle newlines
: handle-newline ( -- )
  set-normal-command
  line @ line-count @ line @ line-offset ! update-line space
  add-to-history revert-history-edits ;

\ Handle delete
: handle-delete ( -- )
  set-normal-command
  line @ line-offset @ 0 > if
    line @ line-buffer @ line @ line-offset @ + dup 1 -
    line @ line-count @ line @ line-offset @ - move
    -1 line @ line-offset +! -1 line @ line-count +!
  then
  update-line ;

\ Handle delete forward
: handle-delete-forward ( -- )
  set-normal-command
  line @ line-offset @ line @ line-count @ < if
    line @ line-buffer @ line @ line-offset @ + dup 1 + swap
    line @ line-count @ line @ line-offset @ - 1 - move
    -1 line @ line-count +!
  then
  update-line ;

\ Move first line into history vector
: move-into-history ( -- )
  here line-history-item-size allot
  line @ line-buffer @ over line-history-header-size +
  line @ line-count @ move
  line @ line-count @ over line-history-entry-length !
  dup line @ line-history-vector @ push-start-vector averts x-history-internal
  line @ line-history-edits-vector @ push-start-vector averts x-history-internal
  line-history-item-size negate allot
  0 line @ line-history-current ! true line @ line-first-line-added ! ;

\ Save current line into history edits
: save-current-edit ( -- )
  here line-history-item-size allot
  line @ line-buffer @ over line-history-header-size +
  line @ line-count @ move
  line @ line-count @ over line-history-entry-length !
  line @ line-history-current @
  line @ line-history-edits-vector @ set-vector averts x-history-internal
  line-history-item-size negate allot ;

\ Read current line from history edits
: read-current-edit ( -- )
  line @ line-history-current @ 0 >= if
    here line-history-item-size allot
    dup line @ line-history-current @
    line @ line-history-edits-vector @ get-vector averts x-history-internal
    dup line-history-entry-length @ line @ line-count !
    line-history-header-size + line @ line-buffer @ line @ line-count @ move
    line-history-item-size negate allot
  then ;

\ Handle up key
: handle-up ( -- )
  set-normal-command
  line @ line-history-current @ line @ line-history-vector @ count-vector 1 - < if
    line @ line-history-current @ -1 = if
      move-into-history
    else
      save-current-edit
    then
    1 line @ line-history-current +! read-current-edit 0 line @ line-offset !
  then
  update-line ;

\ Handle down key
: handle-down ( -- )
  set-normal-command
  line @ line-history-current @ 0 > if
    save-current-edit -1 line @ line-history-current +!
    read-current-edit 0 line @ line-offset !
  then
  update-line ;

\ Handle go to start
: handle-start ( -- ) set-normal-command 0 line @ line-offset ! update-line ;

\ Handle go to end
: handle-end ( -- )
  set-normal-command line @ line-count @ line @ line-offset ! update-line ;

\ Handle bye
: handle-bye ( -- )
  set-normal-command
  line @ line-count @ line @ line-offset ! update-line bye ;

\ Handle go forward
: handle-forward ( -- )
  set-normal-command
  line @ line-offset @ 1 + line @ line-count @ min line @ line-offset !
  update-line ;

\ Handle go backward
: handle-backward ( -- )
  set-normal-command
  line @ line-offset @ 1 - 0 max line @ line-offset ! update-line ;

\ Handle go forward one word
: handle-forward-word ( -- )
  set-normal-command
  line @ line-offset @ find-next-print find-next-ws line @ line-offset !
  update-line ;

\ Handle go backward one word
: handle-backward-word ( -- )
  set-normal-command
  line @ line-offset @ find-prev-last-ws find-prev-last-print
  line @ line-offset ! update-line ;

\ Free space if necessary in the kill vector
: free-kill-vector-space ( -- )
  line @ line-kill-vector @ count-vector
  line @ line-kill-vector @ get-vector-max-count = if
    line @ line-kill-vector @ drop-end-vector averts x-kill-internal
  then ;

\ Add a new kill with text at the given offset and length
: add-new-kill ( offset length -- )
  swap >r >r free-kill-vector-space
  here line-kill-item-size allot
  r> 2dup swap line-kill-entry-length ! line @ line-buffer @ r> +
  2 pick line-kill-header-size + rot move
  line @ line-kill-vector @ push-start-vector averts x-kill-internal
  line-kill-item-size negate allot ;

\ Basic case of killing forward to the end of the line
: handle-kill-forward-line-basic ( -- )
  line @ line-offset @ line @ line-count @ line @ line-offset @ - add-new-kill
  line @ line-offset @ line @ line-count ! ;

\ Add text at the given offset and length to the start of the most recent kill
: add-text-to-kill-start ( offset length -- )
  swap >r >r here line-kill-item-size allot
  dup 0 line @ line-kill-vector @ get-vector averts x-kill-internal
  dup line-kill-header-size + over line-kill-header-size + r@ +
  2 pick line-kill-entry-length @ move
  r@ over line-kill-entry-length +!
  r> line @ line-buffer @ r> + 2 pick line-kill-header-size + rot move
  0 line @ line-kill-vector @ set-vector averts x-kill-internal
  line-kill-item-size negate allot ;

\ Add text at the given offset and length to the end of the most recent kill
: add-text-to-kill-end ( offset length -- )
  >r >r here line-kill-item-size allot
  dup 0 line @ line-kill-vector @ get-vector averts x-kill-internal
  line @ line-buffer @ r> +
  over line-kill-header-size + 2 pick line-kill-entry-length @ +
  r@ move r> over line-kill-entry-length +!
  0 line @ line-kill-vector @ set-vector averts x-kill-internal
  line-kill-item-size negate allot ;

\ Case of killing forward to the end of the line and adding the text to the end
\ of the most recent kill
: handle-kill-forward-line-add ( -- )
  line @ line-offset @
  line @ line-count @ line @ line-offset @ - add-text-to-kill-end ;

\ Check whether there is room for a given length of text in the most recent kill
: check-room-for-kill ( length -- flag )
  >r here line-kill-item-size allot
  dup 0 line @ line-kill-vector @ get-vector averts x-kill-internal
  line-kill-data-size swap line-kill-entry-length @ - r> >=
  line-kill-item-size negate allot ;

\ Handle kill forward to the end of the line
: handle-kill-forward-line ( -- )
  line @ line-offset @ line @ line-count @ < if
    line @ line-last-command-type @ kill-forward-command =
    set-kill-forward-command
    line @ line-kill-vector @ count-vector 0 > and if
      line @ line-count @ line @ line-offset @ check-room-for-kill if
        handle-kill-forward-line-add
      else
        handle-kill-forward-line-basic
      then
    else
      handle-kill-forward-line-basic
    then
  else
    set-kill-forward-command
  then
  update-line ;

\ Remove text from the input buffer
: remove-text ( offset length -- )
  2dup + line @ line-buffer @ + 2 pick line @ line-buffer @ +
  3 roll 3 pick + line @ line-count @ swap - move
  negate line @ line-count +! ;

\ Insert text into the input buffer
: insert-text ( addr length offset -- )
  line @ line-buffer @ over + line @ line-buffer @ 2 pick + 3 pick +
  line @ line-count @ 3 pick - move
  rot line @ line-buffer @ rot + 2 pick move line @ line-count +! ;

\ Basic case of killing forward to the start of the next whitespace
: handle-kill-to-next-space-basic ( -- )
  line @ line-offset @ find-next-print find-next-ws line @ line-offset @ -
  dup 0 > if
    line @ line-offset @ swap 2dup add-new-kill remove-text
  else
    drop
  then ;

\ Case of killing forward to the start of the next whitespace and adding the
\ text to the end of the most recent kill
: handle-kill-to-next-space-add ( -- )
  line @ line-offset @ find-next-print find-next-ws line @ line-offset @ -
  dup 0 > if
    dup check-room-for-kill if
      line @ line-offset @ swap 2dup add-text-to-kill-end remove-text
    else
      line @ line-offset @ swap 2dup add-new-kill remove-text
    then
  else
    drop
  then ;

\ Handle kill forward to the end of the next word
: handle-kill-to-next-space ( -- )
  line @ line-offset @ line @ line-count @ < if
    line @ line-last-command-type @ kill-forward-command =
    set-kill-forward-command
    line @ line-kill-vector @ count-vector 0 > and if
      handle-kill-to-next-space-add
    else
      handle-kill-to-next-space-basic
    then
  else
    set-kill-forward-command
  then
  update-line ;

\ Basic case of killing backward to the start of the previous word
: handle-kill-to-prev-space-basic ( -- )
  line @ line-offset @ find-prev-last-ws find-prev-last-print
  dup line @ line-offset @ < if
    line @ line-offset @ over - dup >r 2dup add-new-kill remove-text
    r> negate line @ line-offset +!
  else
    drop
  then ;

\ Case of killing backward to the start of the previous word and adding the
\ text to the start of the most recent kill
\ Basic case of killing forward to the start of the previous word
: handle-kill-to-prev-space-add ( -- )
  line @ line-offset @ find-prev-last-ws find-prev-last-print
  dup line @ line-offset @ < if
    line @ line-offset @ over - dup >r dup check-room-for-kill if
      2dup add-text-to-kill-start remove-text
    else
      2dup add-new-kill remove-text
    then
    r> negate line @ line-offset +!
  else
    drop
  then ;

\ Handle kill backward to the start of the previous word
: handle-kill-to-prev-space ( -- )
  line @ line-offset @ 0 > if
    line @ line-last-command-type @ kill-backward-command =
    set-kill-backward-command
    line @ line-kill-vector @ count-vector 0 > and if
      handle-kill-to-prev-space-add
    else
      handle-kill-to-prev-space-basic
    then
  else
    set-kill-backward-command
  then
  update-line ;

\ Insert yanked text
: insert-yank ( -- )
  here line-kill-item-size allot
  dup 0 line @ line-kill-vector @ get-vector averts x-kill-internal
  dup line-kill-header-size +
  tib-size line @ line-count @ - rot line-kill-entry-length @ min dup >r
  line @ line-offset @ insert-text r> line @ line-offset +!
  line-kill-item-size negate allot ;

\ Remove yanked text
: remove-yank ( -- )
  here line-kill-item-size allot
  dup 0 line @ line-kill-vector @ get-vector averts x-kill-internal
  dup line-kill-entry-length @ line @ line-offset @ over - swap remove-text
  line-kill-entry-length @ negate line @ line-offset +!
  line-kill-item-size negate allot ;

\ Cycle through yanked text
: cycle-yank ( -- )
  here line-kill-item-size allot
  dup line @ line-kill-vector @ pop-start-vector averts x-kill-internal
  line @ line-kill-vector @ push-end-vector averts x-kill-internal
  line-kill-item-size negate allot ;

\ Handle yanking the most recent kill
: handle-yank ( -- )
  set-yank-command
  line @ line-kill-vector @ count-vector 0 > if
    insert-yank
  then
  update-line ;

\ Handle yanking the previous kill
: handle-yank-prev ( -- )
  line @ line-last-command-type @ yank-command = if
    set-yank-command
    line @ line-kill-vector @ count-vector 1 > if
      remove-yank cycle-yank insert-yank
    then
    update-line
  else
    handle-yank
  then ;

\ Test whether a word is an initial match for a string
: word-prefix? ( c-addr u xt -- matches )
  word>name 2 pick swap <= if
    swap equal-case-chars?
  else
    drop 2drop false
  then ;

\ Get whether a completion is actually unique for a wordlist
: wordlist-rest-completion? ( c-addr u xt -- xt duplicate )
  dup word>next begin
    dup 0 <> if
      3 pick 3 pick 2 pick word-prefix? if
        2drop 2drop 0 true true
      else
        word>next false
      then
    else
      drop rot rot 2drop false true
    then
  until ;

\ Get whether a completion is unique for a wordlist
: wordlist-completion? ( c-addr u wid -- xt duplicate )
  wordlist>first begin
    dup 0 <> if
      2 pick 2 pick 2 pick word-prefix? if
        wordlist-rest-completion? true
      else
        word>next false
      then
    else
      drop 2drop 0 false true
    then
  until ;

\ Test whether there are no more potential completions
: rest-completion? ( x*u2 c-addr u1 u2 xt -- xt duplicate )
  begin
    over 0 > if
      swap 1- swap 3 pick 3 pick 6 roll wordlist-completion? swap 0 <> and if
        drop 2 + drops 0 true true
      else
	false
      then
    else
      nip nip nip false true
    then
  until ;

\ Test whether a given prefix has only one possible completion
: only-completion? ( c-addr u -- xt duplicate )
  2>r get-order 2r> rot begin
    dup 0 > if
      1 - 2 pick 2 pick 5 roll wordlist-completion? if
        drop 2 + drops 0 true true
      else dup 0 <> if
        rest-completion? true
      else
        drop false
      then then
    else
      drop 2drop 0 false true
    then
  until ;

\ Display available completions for a single wordlist
: display-wordlist-completions ( c-addr u wid -- )
  wordlist>first begin
    dup 0 <> if
      2 pick 2 pick 2 pick word-prefix? if
        dup word>name type space
      then
      word>next false
    else
      drop 2drop true
    then
  until ;

\ Display available completions for all wordlists
: display-completions ( c-addr u -- )
  cr
  >r >r get-order r> r> rot begin
    dup 0 > if
      1 - 2 pick 2 pick 5 roll display-wordlist-completions false
    else
      drop 2drop true
    then
  until
  cr
  config-line update-line ;

\ Insert a completion
: insert-completion ( c-addr u -- )
  line @ line-offset @ find-prev-last-print dup line @ line-offset @ over -
  remove-text 2dup + line @ line-offset ! insert-text ;  

\ Handle autocomplete
: handle-tab ( -- )
  set-normal-command
  line @ line-complete-enable @ if
    line @ line-offset @ find-prev-last-print dup line @ line-offset @ < if
      line @ line-buffer @ over + line @ line-offset @ rot -
      2dup only-completion? not if
        ?dup if word>name insert-completion then 2drop
      else
        drop display-completions
      then
    else
      drop
    then
  then
  update-line ;

\ Refresh
: handle-refresh ( -- ) update-line ;

\ Handle a normal key
: handle-normal-key ( c -- )
  set-normal-command
  line @ line-count @ tib-size < if
    line @ line-buffer @ line @ line-offset @ + dup 1 +
    line @ line-count @ line @ line-offset @ - move
    line @ line-buffer @ line @ line-offset @ + c! 1 line @ line-offset +!
    1 line @ line-count +!
  else
    drop
  then
  update-line ;

\ Handle special control keys; returns whether to stop reading
: handle-ctrl-special ( source -- stop )
  key update-terminal-size case
    [char] ; of
      key update-terminal-size case
        [char] 5 of
          key update-terminal-size case
            [char] C of handle-forward-word false endof
            [char] D of handle-backward-word false endof
	    dup >r set-key false r>
          endcase
        endof
	dup >r set-key false r>
      endcase
    endof
    dup >r set-key false r>
  endcase ;

\ Handle special keys; returns whether to stop reading
: handle-special ( source -- stop )
  key update-terminal-size case
    [char] C of handle-forward false endof
    [char] D of handle-backward false endof
    [char] A of handle-up false endof
    [char] B of handle-down false endof
    [char] 3 of
      key update-terminal-size case
        [char] ~ of handle-delete-forward false endof
	dup >r set-key false r>
      endcase
    endof
    [char] 1 of handle-ctrl-special endof
    dup >r set-key false r>
  endcase ;

\ Handle keypresses including escape characters; returns whether to stop reading
: handle-escape ( source -- stop )
  key update-terminal-size case
    [char] [ of handle-special endof
    [char] f of handle-forward-word false endof
    [char] b of handle-backward-word false endof
    [char] y of handle-yank-prev false endof
    [char] d of handle-kill-to-next-space false endof
    delete of handle-kill-to-prev-space false endof
    dup >r set-key false r>
  endcase ;

\ Handle keypresses; returns whether to stop reading
: handle-key ( -- stop )
  key update-terminal-size case
    newline of handle-newline true endof
    delete of handle-delete false endof
    tab of handle-tab false endof
    escape of handle-escape endof
    ctrl-a of handle-start false endof
    ctrl-d of handle-bye false endof \ HANDLE-BYE should never return
    ctrl-e of handle-end false endof
    ctrl-f of handle-forward false endof
    ctrl-b of handle-backward false endof
    ctrl-k of handle-kill-forward-line false endof
    ctrl-l of handle-refresh false endof
    ctrl-w of handle-kill-to-prev-space false endof
    ctrl-y of handle-yank false endof
    dup $20 < if false swap else dup handle-normal-key false swap then
  endcase ;

\ Edit a line of text
: edit-line ( -- ) reset-line config-line begin handle-key until ;

\ Read a line of text
: (accept) ( addr bytes1 -- bytes2 )
  edit-line line @ line-count @ min line @ line-buffer @ swap rot swap
  dup >r cmove r> ;

\ Create a line editor for the main task
new-line line !

\ Set accept hook
' (accept) 'accept !

base ! set-current set-order

