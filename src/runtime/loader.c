/* Copyright (c) 2019, Travis Bemann
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 * 
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of the copyright holder nor the names of its
 *    contributors may be used to endorse or promote products derived from
 *    this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE. */

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <stdint.h>
#include "hf/common.h"
#include "hf/inner.h"
#include "hf/prim.h"

/* Declarations */

/* Read a file into a single buffer */
void* hf_read_file(FILE* file, size_t* size);

/* Verify the image type against the compiled configuration */
void* hf_verify_image_type(void* current);

/* Parse headers */
void* hf_parse_headers(hf_global_t* global, void* current);

/* Load code */
void hf_load_code(hf_global_t* global, void* current, size_t size);

/* Relocate code */
void hf_relocate(hf_global_t* global, hf_full_token_t start_token,
		 void* start_address);

/* Definitions */

/* Load an image from a file. */
void hf_load_image(hf_global_t* global, char* path) {
  FILE* file = fopen(path, "r");
  void* buffer;
  void* current;
  hf_full_token_t start_token = global->word_count;
  void* start_address;
  size_t size;
  if(!file) {
    fprintf(stderr, "Unable to open image!\n");
    exit(1);
  }
  buffer = hf_read_file(file, &size);
  fclose(file);
  current = hf_verify_image_type(buffer);
  current = hf_parse_headers(global, current);
  start_address = global->user_space_current;
  hf_load_code(global, current, (buffer + size) - current);
  hf_relocate(global, start_token, start_address);
}

/* Read a file into a single buffer */
void* hf_read_file(FILE* file, size_t* size) {
  void* buffer;
  size_t buffer_size = 2048;
  size_t prev_buffer_size = 0;
  size_t read_size = 0;
  if(!(buffer = malloc(buffer_size))) {
    fprintf(stderr, "Unable to allocate memory!\n");
    exit(1);
  }
  while((read_size = fread(buffer + prev_buffer_size, sizeof(hf_byte_t),
			buffer_size - prev_buffer_size, file))
	== (buffer_size - prev_buffer_size)) {
    prev_buffer_size = buffer_size;
    buffer_size *= 2;
    if(!(buffer = realloc(buffer, buffer_size))) {
      fprintf(stderr, "Unable to resize buffer!\n");
      exit(1);
    }
  }
  if(feof(file)) {
    if(!(buffer = realloc(buffer, prev_buffer_size + read_size))) {
      fprintf(stderr, "Unable to resize buffer!\n");
      exit(1);
    }
    *size = prev_buffer_size + read_size;
  } else {
    fprintf(stderr, "Error reading image!\n");
    exit(1);
  }
  return buffer;
}

/* Verify the image type against the compiled configuration */
void* hf_verify_image_type(void* current) {
  hf_byte_t cell_type;
  hf_byte_t token_type;
  hf_cell_t error = HF_FALSE;
  cell_type = *(hf_byte_t*)current;
  current += sizeof(hf_byte_t);
  token_type = *(hf_byte_t*)current;
  current += sizeof(hf_byte_t);
#ifdef CELL_16
  if(cell_type != 0) {
    error = HF_TRUE;
  }
#else
#ifdef CELL_32
  if(cell_type != 1) {
    error = HF_TRUE;
  }
#else /* CELL_64 */
  if(cell_type != 2) {
    error = HF_TRUE;
  }
#endif
#endif
  if(error) {
    fprintf(stderr, "Cell size of image does not match configuration!\n");
    exit(1);
  }
#ifdef TOKEN_8_16
  if(token_type != 0) {
    error = HF_TRUE;
  }
#else
#ifdef TOKEN_16
  if(token_type != 1) {
    error = HF_TRUE;
  }
#else
#ifdef TOKEN_16_32
  if(token_type != 2) {
    error = HF_TRUE;
  }
#else /* TOKEN_32 */
  if(token_type != 3) {
    error = HF_TRUE;
  }
#endif
#endif
#endif
  if(error) {
    fprintf(stderr, "Token type of image does not match configuration!\n");
    exit(1);
  }
  return current;
}

/* Parse headers */
void* hf_parse_headers(hf_global_t* global, void* current) {
  hf_byte_t type;
  while((type = *(hf_byte_t*)current)) {
    hf_cell_t token;
    hf_word_t* word;
    hf_cell_t flags;
    hf_cell_t offset;
    hf_cell_t name_length;
    hf_byte_t* name;
    hf_byte_t* name_dest;
    char* name_copy;
    current += sizeof(hf_byte_t);
    token = *(hf_cell_t*)current;
    current += sizeof(hf_cell_t);
    flags = *(hf_cell_t*)current;
    current += sizeof(hf_cell_t);
    offset = *(hf_cell_t*)current;
    current += sizeof(hf_cell_t);
    name_length = *(hf_cell_t*)current;
    current += sizeof(hf_cell_t);
    name = (hf_byte_t*)current;
    current += name_length;
    name_copy = malloc(name_length + 1);
    memcpy(name_copy, name, name_length);
    name_copy[name_length] = 0;
    printf("token: %lld name: %s flags: %lld offset: %lld\n",
	   (uint64_t)token, name_copy, (uint64_t)flags, (uint64_t)offset);
    free(name_copy);
    if(token != global->word_count) {
      fprintf(stderr, "Unexpected token value in image!\n");
      exit(1);
    }
    if((type != 1) && (type != 2)) {
      fprintf(stderr, "Unexpected word type!\n");
      exit(1);
    }
    word = hf_new_word(global, hf_new_token(global));
    name_dest = (hf_byte_t*)hf_allocate(global, name_length);
    if(type == 1) {
      word->primitive = hf_prim_enter;
      word->secondary = (hf_token_t*)offset;
      word->data = NULL;
    } else {
      word->primitive = hf_prim_do_create;
      word->secondary = NULL;
      word->data = (void*)offset;
    }
    word->flags = flags;
    word->name_length = name_length;
    word->name = name_dest;
    memcpy(name_dest, name, name_length);
    word->next = global->word_count > 1 ? global->word_count - 2 : 0;
  }
  return current + 1;
}

/* Load code */
void hf_load_code(hf_global_t* global, void* current, size_t size) {
  memcpy(global->user_space_current, current, size);
  global->user_space_current += size;
}

/* Relocate code */
void hf_relocate(hf_global_t* global, hf_full_token_t start_token,
		 void* start_address) {
  hf_full_token_t token = start_token;
  while(token < global->word_count) {
    hf_word_t* word = global->words + token;
    if(word->primitive == hf_prim_enter) {
      char* name_copy = malloc(word->name_length + 1);
      memcpy(name_copy, word->name, word->name_length);
      name_copy[word->name_length] = 0;
      printf("relocating token: %lld name: %s offset: %lld new offset: %lld\n",
	     (uint64_t)token, name_copy, (uint64_t)word->secondary,
	     (uint64_t)word->secondary +
	     (uint64_t)start_address - (uint64_t)global->user_space_start);
      free(name_copy);
      
      word->secondary =
	(hf_token_t*)((hf_cell_t)word->secondary + (hf_cell_t)start_address);

      hf_token_t* current = word->secondary;
      hf_token_t parsed_token;
      while((parsed_token = *current++) != HF_PRIM_END) {
	/*	printf("found token: %lld\n", (uint64_t)parsed_token); */
#ifdef TOKEN_8_16
	if(parsed_token & 0x80) {
	  *current++;
	}
#else
#ifdef TOKEN_16_32
	if(parsed_token & 0x8000) {
	  *current++;
	}
#endif
#endif
	if(parsed_token == HF_PRIM_BRANCH || parsed_token == HF_PRIM_0BRANCH) {
	  *(hf_cell_t*)current += (hf_cell_t)start_address;
	  current = (hf_token_t*)((void*)current + sizeof(hf_cell_t));
	} else if(parsed_token == HF_PRIM_LIT) {
	  current = (hf_token_t*)((void*)current + sizeof(hf_cell_t));
	} else if(parsed_token == HF_PRIM_DATA) {
	  hf_cell_t size = *(hf_cell_t*)current;
	  current = (hf_token_t*)((void*)current + sizeof(hf_cell_t) + size);
	}
      }
    } else if (word->primitive == hf_prim_do_create) {
      char* name_copy = malloc(word->name_length + 1);
      memcpy(name_copy, word->name, word->name_length);
      name_copy[word->name_length] = 0;
      printf("relocating token: %lld name: %s offset: %lld new offset: %lld\n",
	     (uint64_t)token, name_copy, (uint64_t)word->data,
	     (uint64_t)word->data +
	     (uint64_t)start_address - (uint64_t)global->user_space_start);
      free(name_copy);
      word->data =
	(hf_token_t*)((hf_cell_t)word->data + (hf_cell_t)start_address);
    } else if (word->primitive == hf_prim_do_does) {
      word->data =
	(hf_token_t*)((hf_cell_t)word->data + (hf_cell_t)start_address);
      word->secondary =
	(hf_token_t*)((hf_cell_t)word->secondary + (hf_cell_t)start_address);
    }
    token++;
  }
}
