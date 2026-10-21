# Fancy Kinematic Test
I made my character controller as per the requirements. My special mechanic is swimming, in a system tolerating multiple different liquids. The test scene includes water and falling sand as Water is likely the most common liquid to encounter and Falling Sand is a very thick liquid. Falling sand is opaque, but it does make it harder to swim in, as it is in real life, but feel free to make it transparent for testing purposes. 

## Liquid and Moving Platforms
The character stay attached to the moving platform while entering liquids. If it would actually have a model, some crouching or floor grabbing animation while underwater on a moving platform would be amazing. If the character jumps though, they begin to swim, leaving the moving platform. Moving platforms themselves are not affected by the liquids. 

## Camera Issues
The camera itself is a normal 3rd person camera and works fine, but I made my liquid cubes so when the camera gets into the liquid, its mesh is not visible, so this is likely not how one should do a liquid in unity... or, there is a setting I missed in materials...

## More Complicated Liquid
The liquids I made as examples are pretty simple. If one wants to make more complex ones they could simply make children of Liquid. Liquid only uses Monobehavior functions and thus can be overridden in the same way. 
