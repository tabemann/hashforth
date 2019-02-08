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
#include <strings.h>
#include <stdint.h>
#include "hf/common.h"
#include "hf/inner.h"
#include "hf/prim.h"
#include "hf/sys.h"

/* Declarations */

/* Read a file into a single buffer */
void* hf_read_file(FILE* file, size_t* size);

/* Verify the image type against the compiled configuration */
void* hf_verify_image_type(void* current);

/* Parse headers */
void* hf_parse_headers(hf_global_t* global, void* current,
		       void** user_space_current);

/* Load code */
void hf_load_code(hf_global_t* global, void* current, size_t size,
		  void** user_space_current);

/* Relocate code */
void hf_relocate(hf_global_t* global, hf_full_token_t start_token,
		 void* start_address, void* user_space);

/* Load storage */
void* hf_load_storage(hf_global_t* global, void* current, size_t size,
		      void** user_space_current);

/* Find name data */
void* hf_find_name_data(hf_global_t* global, char* name);

/* Definitions */

/* Load an image from a file. */
void hf_load_image(hf_global_t* global, char* path) {
  FILE* file = fopen(path, "r");
  void* buffer;
  void* current;
  hf_full_token_t start_token = global->word_count;
  void* start_address;
  size_t size;
  size_t mem_size;
  hf_cell_t max_word_count;
  hf_cell_t max_return_stack_count;
  void* user_space;
  void* mem_space;
  void* mem_current;
  size_t user_image_size;
  void* storage;
  hf_cell_t* user_space_current_var;
  hf_cell_t* storage_var;
  hf_cell_t* storage_size_var;
  if(!file) {
    fprintf(stderr, "Unable to open image!\n");
    exit(1);
  }
  buffer = hf_read_file(file, &size);
  fclose(file);
  current = hf_verify_image_type(buffer);
  mem_size = *(hf_cell_t*)current;
  current += sizeof(hf_cell_t);
  max_word_count = *(hf_cell_t*)current;
  current += sizeof(hf_cell_t);
  max_return_stack_count = *(hf_cell_t*)current;
  current += sizeof(hf_cell_t);
  if(!(mem_current = mem_space = malloc(mem_size))) {
    fprintf(stderr, "Unable to allocate user space!\n");
    exit(1);
  }
  global->words = (hf_word_t*)mem_current;
  global->word_space_count = max_word_count;
  mem_current += max_word_count * sizeof(hf_word_t);
  global->return_stack = mem_space + mem_size;
#ifdef TRACE
  global->return_stack_base = global->return_stack;
#endif
  global->data_stack =
    (mem_space + mem_size) - (max_return_stack_count * sizeof(hf_cell_t));
#ifdef STACK_TRACE
  global->data_stack_base = global->old_data_stack_base = global->data_stack;
#endif
  global->std_service_space_count = HF_MAX_STD_SERVICES;
  global->std_services = (hf_sys_t*)mem_current;
  for(int i = 0; i < HF_MAX_STD_SERVICES; i++) {
    global->std_services[i].defined = HF_FALSE;
  }
  mem_current += HF_MAX_STD_SERVICES * sizeof(hf_sys_t);
  global->nstd_service_space_count = HF_MAX_NSTD_SERVICES;
  global->nstd_services = (hf_sys_t*)mem_current;
  for(int i = 0; i < HF_MAX_NSTD_SERVICES; i++) {
    global->nstd_services[i].defined = HF_FALSE;
  }
  mem_current += HF_MAX_NSTD_SERVICES * sizeof(hf_sys_t);
  hf_register_prims(global, &mem_current);
  hf_register_services(global, &mem_current);
  user_space = mem_current;
  current = hf_parse_headers(global, current, &mem_current);
  user_image_size = *(hf_cell_t*)current;
  current += sizeof(hf_cell_t);
  start_address = mem_current;
  hf_load_code(global, current, user_image_size, &mem_current);
  current += user_image_size;
  hf_relocate(global, start_token, start_address, user_space);
  storage = mem_current;
  hf_load_storage(global, current, (buffer + size) - current, &mem_current);
  *(--global->data_stack) = (hf_cell_t)storage;
  *(--global->data_stack) = (hf_cell_t)((buffer + size) - current);
  *(--global->data_stack) = (hf_cell_t)mem_current;
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
void* hf_parse_headers(hf_global_t* global, void* current,
		       void** user_space_current) {
  hf_byte_t type;
  while((type = *(hf_byte_t*)current)) {
    hf_cell_t token;
    hf_word_t* word;
    hf_cell_t offset;
    current += sizeof(hf_byte_t);
    token = *(hf_cell_t*)current;
    current += sizeof(hf_cell_t);
    offset = *(hf_cell_t*)current;
    current += sizeof(hf_cell_t);
#ifdef DUMP_LOAD
    fprintf(stderr, "loading token: %lld offset: %lld\n",
	    (uint64_t)token, (uint64_t)offset);
#endif
    if(token != global->word_count) {
      fprintf(stderr, "Unexpected token value in image!\n");
      exit(1);
    }
    if((type != 1) && (type != 2)) {
      fprintf(stderr, "Unexpected word type!\n");
      exit(1);
    }
    word = hf_new_word(global, hf_new_token(global));
    if(type == 1) {
      word->primitive = hf_prim_enter;
      word->secondary = (hf_token_t*)offset;
      word->data = NULL;
    } else {
      word->primitive = hf_prim_do_create;
      word->secondary = NULL;
      word->data = (void*)offset;
    }
  }
  return current + 1;
}

/* Load code */
void hf_load_code(hf_global_t* global, void* current, size_t size,
		  void** user_space_current) {
  memcpy(*user_space_current, current, size);
  *user_space_current += size;
}

/* Relocate code */
void hf_relocate(hf_global_t* global, hf_full_token_t start_token,
		 void* start_address, void* user_space) {
  hf_full_token_t token = start_token;
  while(token < global->word_count) {
    hf_word_t* word = global->words + token;
    if(word->primitive == hf_prim_enter) {
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

/* Load storage */
void* hf_load_storage(hf_global_t* global, void* current, size_t size,
		      void** user_space_current) {
  memcpy(*user_space_current, current, size);
#ifdef DUMP_LOAD
  printf("loading storage bytes: %lld\n", (uint64_t)size);
#endif
  *user_space_current += size;
}
