from nn import NNTraining

training = NNTraining(inputFile='sino-ml-demon.json', outputFile='sino-ml-demon.txt')
training.run(numFeatures=50*10, numClasses=2, numHidden1=64, numSteps=10000)
