sprite-sheet-builder
====================

A program combine sprite frame images into sprite sheets.

USAGE build-sprite-sheets input-folder output-folder

The input folder is expected to have one subfolder for each sprite sheet to make. 

In each subfolder should be a PNG for each frame of each sequence. 

Each PNG's filename should start with the sequence name, then an underscore, then the frame number. The rest of the filename is ignored. Example "jump_0002_other-stuff-is-ignored.png". This is to match up with the format used by Photoshop's File > Scripts > Export Layers To Files command.
