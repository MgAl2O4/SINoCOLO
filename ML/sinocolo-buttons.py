from nn import NNTraining

training = NNTraining(inputFile='sino-ml-buttons.json', outputFile='sino-ml-buttons.txt')
training.run(numFeatures=16*8, numClasses=6)
