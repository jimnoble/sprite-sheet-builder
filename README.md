Sprite Sheet Builder
====================

A program to combine sprite frame images into sprite sheets.

USAGE build-sprite-sheets input-folder output-folder

The input folder is expected to have one subfolder for each sprite sheet to make. 

In each subfolder should be a .gif or a series of .pngs for each sequence. 

Each .gif's filename should be the name of the sequence. Example "jump.gif".

Each .png's filename should start with the sequence name, then an underscore, then the frame number. The rest of the filename is ignored. Example "jump_0002_other-stuff-is-ignored.png". This is to match up with the format used by Photoshop's File > Scripts > Export Layers To Files command.

Once run, the output folder will contain all sprite sheets as .png, as well as a .json file describing the location and size of each frame of each sequence of each sprite sheet. This information is useful for animating within the game.

All frames are separated from each other and the edge of the screen by one empty pixel.

Maximum output sprite sheet size is 1024x1024, to match up with OpenGL's maximum texture size.
