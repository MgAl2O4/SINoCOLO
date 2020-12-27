from nn import NNTraining

training = NNTraining(inputFile='sino-ml-weapons.json', outputFile='sino-ml-weapons.txt')
training.run(numFeatures=10*10, numClasses=4)
